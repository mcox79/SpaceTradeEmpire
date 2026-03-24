"""Compare current screenshots against baseline PNGs.

Usage:
    python compare_screenshots.py --current reports/screenshot/full/ --baseline reports/baselines/full/
    python compare_screenshots.py --current reports/screenshot/quick/ --baseline reports/baselines/quick/
    python compare_screenshots.py --current ... --baseline ... --metric ssim  # perceptual SSIM comparison

Output: JSON to stdout with per-image comparison results.
Exit codes: 0 = all pass, 1 = any regression, 2 = missing dependency.
"""

import argparse
import json
import math
import os
import re
import sys

try:
    from PIL import Image, ImageChops
except ImportError:
    print("ERROR: Pillow not installed. Run: pip install Pillow", file=sys.stderr)
    sys.exit(2)

# Optional: numpy for SSIM computation (much more perceptually accurate than MAD)
_HAS_NUMPY = False
try:
    import numpy as np
    _HAS_NUMPY = True
except ImportError:
    pass


def extract_phase_label(filename):
    """Strip tick/timestamp suffix to get the phase label for matching.

    Examples:
        boot_0001_17730640.png -> boot
        dock_market_0042_17730641.png -> dock_market
        npc_combat_f01_0099_17730641.png -> npc_combat_f01
    """
    stem = os.path.splitext(filename)[0]
    # Remove trailing _DIGITS groups (tick and timestamp)
    cleaned = re.sub(r"(_\d+)+$", "", stem)
    return cleaned


def compute_ssim(baseline_img, current_img):
    """Compute Structural Similarity Index (SSIM) between two images.

    SSIM is far more perceptually accurate than pixel-level MAD.
    It measures luminance, contrast, and structure similarity,
    closely matching human visual perception. This reduces false
    positives from anti-aliasing, particle positions, and minor
    rendering variance that MAD flags incorrectly.

    Returns a value in [0.0, 1.0] where 1.0 = identical.
    Requires numpy. Falls back to MAD-based metrics if unavailable.

    Reference: Wang et al., "Image Quality Assessment: From Error Visibility
    to Structural Similarity", IEEE TIP 2004.
    """
    if not _HAS_NUMPY:
        return None

    # Convert to grayscale float arrays (SSIM is typically computed on luminance)
    b_gray = np.array(baseline_img.convert("L"), dtype=np.float64)
    c_gray = np.array(current_img.convert("L"), dtype=np.float64)

    if b_gray.shape != c_gray.shape:
        return None

    # SSIM constants (from the original paper)
    C1 = (0.01 * 255) ** 2  # stabilizer for luminance
    C2 = (0.03 * 255) ** 2  # stabilizer for contrast

    # Window-based SSIM (8x8 blocks for efficiency)
    window_size = 8
    h, w = b_gray.shape
    if h < window_size or w < window_size:
        # Image too small for windowed SSIM, compute global
        mu_b = b_gray.mean()
        mu_c = c_gray.mean()
        sigma_b_sq = b_gray.var()
        sigma_c_sq = c_gray.var()
        sigma_bc = ((b_gray - mu_b) * (c_gray - mu_c)).mean()

        num = (2 * mu_b * mu_c + C1) * (2 * sigma_bc + C2)
        den = (mu_b ** 2 + mu_c ** 2 + C1) * (sigma_b_sq + sigma_c_sq + C2)
        return float(num / den) if den != 0 else 1.0

    # Crop to multiple of window_size
    h_crop = (h // window_size) * window_size
    w_crop = (w // window_size) * window_size
    b_gray = b_gray[:h_crop, :w_crop]
    c_gray = c_gray[:h_crop, :w_crop]

    # Reshape into blocks
    b_blocks = b_gray.reshape(h_crop // window_size, window_size, w_crop // window_size, window_size)
    c_blocks = c_gray.reshape(h_crop // window_size, window_size, w_crop // window_size, window_size)

    # Per-block statistics
    mu_b = b_blocks.mean(axis=(1, 3))
    mu_c = c_blocks.mean(axis=(1, 3))
    sigma_b_sq = b_blocks.var(axis=(1, 3))
    sigma_c_sq = c_blocks.var(axis=(1, 3))

    # Covariance
    b_centered = b_blocks - mu_b[:, np.newaxis, :, np.newaxis]
    c_centered = c_blocks - mu_c[:, np.newaxis, :, np.newaxis]
    sigma_bc = (b_centered * c_centered).mean(axis=(1, 3))

    # SSIM per block
    num = (2 * mu_b * mu_c + C1) * (2 * sigma_bc + C2)
    den = (mu_b ** 2 + mu_c ** 2 + C1) * (sigma_b_sq + sigma_c_sq + C2)
    ssim_map = num / np.where(den == 0, 1, den)

    return float(ssim_map.mean())


def compute_metrics(baseline_img, current_img, use_ssim=False):
    """Compute comparison metrics between two images.

    When use_ssim=True and numpy is available, adds SSIM (Structural Similarity
    Index) which is far more perceptually accurate than pixel-level MAD.
    """
    # Ensure same size
    if baseline_img.size != current_img.size:
        current_img = current_img.resize(baseline_img.size, Image.LANCZOS)

    # Convert to RGB (drop alpha if present)
    b = baseline_img.convert("RGB")
    c = current_img.convert("RGB")

    # Pixel-by-pixel difference
    diff = ImageChops.difference(b, c)
    diff_data = list(diff.getdata())

    total_pixels = len(diff_data)
    if total_pixels == 0:
        result = {"mad": 0.0, "max_diff": 0, "changed_pct": 0.0}
        if use_ssim:
            result["ssim"] = 1.0
        return result

    # Mean Absolute Difference (normalized to 0.0-1.0)
    sum_diff = 0.0
    max_diff = 0
    changed_count = 0
    threshold = int(255 * 0.05)  # 5% of max channel value

    for r, g, b_val in diff_data:
        pixel_diff = r + g + b_val
        sum_diff += pixel_diff
        pixel_max = max(r, g, b_val)
        if pixel_max > max_diff:
            max_diff = pixel_max
        if pixel_max > threshold:
            changed_count += 1

    mad = sum_diff / (total_pixels * 3 * 255)  # normalize
    changed_pct = changed_count / total_pixels * 100.0

    result = {
        "mad": round(mad, 6),
        "max_diff": max_diff,
        "changed_pct": round(changed_pct, 2),
    }

    # Add SSIM if requested
    if use_ssim:
        ssim_val = compute_ssim(b, c)
        if ssim_val is not None:
            result["ssim"] = round(ssim_val, 6)
        else:
            result["ssim"] = None
            result["ssim_error"] = "numpy not available — install with: pip install numpy"

    return result


def classify(metrics, use_ssim=False):
    """Return PASS / WARN / FAIL based on metrics.

    When SSIM is available, use it as the primary signal:
      SSIM >= 0.95 → PASS (perceptually identical)
      SSIM >= 0.85 → WARN (minor differences)
      SSIM <  0.85 → FAIL (significant change)

    SSIM thresholds are tuned for game screenshots where particle
    positions, anti-aliasing, and minor rendering variance are expected.
    Falls back to MAD-based classification when SSIM is unavailable.
    """
    ssim = metrics.get("ssim")

    if use_ssim and ssim is not None:
        if ssim >= 0.95:
            return "PASS"
        elif ssim >= 0.85:
            return "WARN"
        else:
            return "FAIL"

    # Fallback: MAD-based classification
    mad = metrics["mad"]
    changed_pct = metrics["changed_pct"]

    if mad < 0.02 and changed_pct < 5.0:
        return "PASS"
    elif mad < 0.05 or changed_pct < 15.0:
        return "WARN"
    else:
        return "FAIL"


def find_matching_current(phase_label, current_files):
    """Find the current screenshot that matches the given phase label."""
    for f in current_files:
        if extract_phase_label(f) == phase_label:
            return f
    return None


def main():
    parser = argparse.ArgumentParser(description="Compare screenshots against baselines")
    parser.add_argument("--current", required=True, help="Directory with current screenshots")
    parser.add_argument("--baseline", required=True, help="Directory with baseline screenshots")
    parser.add_argument("--threshold-mad", type=float, default=0.02, help="MAD threshold for PASS")
    parser.add_argument("--threshold-pct", type=float, default=5.0, help="Changed%% threshold for PASS")
    parser.add_argument(
        "--metric", choices=["mad", "ssim"], default="mad",
        help="Primary comparison metric: 'mad' (pixel-level, default) or 'ssim' (perceptual, requires numpy)"
    )
    args = parser.parse_args()

    use_ssim = args.metric == "ssim"
    if use_ssim and not _HAS_NUMPY:
        print("WARNING: --metric ssim requires numpy. Install with: pip install numpy", file=sys.stderr)
        print("WARNING: Falling back to MAD-based comparison.", file=sys.stderr)
        use_ssim = False

    if not os.path.isdir(args.baseline):
        print(f"ERROR: Baseline directory not found: {args.baseline}", file=sys.stderr)
        sys.exit(2)

    if not os.path.isdir(args.current):
        print(f"ERROR: Current directory not found: {args.current}", file=sys.stderr)
        sys.exit(2)

    baseline_pngs = sorted(f for f in os.listdir(args.baseline) if f.lower().endswith(".png"))
    current_pngs = sorted(f for f in os.listdir(args.current) if f.lower().endswith(".png"))

    if not baseline_pngs:
        print("ERROR: No baseline PNGs found", file=sys.stderr)
        sys.exit(2)

    results = []
    any_fail = False

    for baseline_file in baseline_pngs:
        phase = extract_phase_label(baseline_file)
        current_file = find_matching_current(phase, current_pngs)

        if current_file is None:
            result = {
                "baseline": baseline_file,
                "current": None,
                "phase": phase,
                "verdict": "FAIL",
                "reason": "no matching current screenshot",
            }
            any_fail = True
        else:
            baseline_img = Image.open(os.path.join(args.baseline, baseline_file))
            current_img = Image.open(os.path.join(args.current, current_file))
            metrics = compute_metrics(baseline_img, current_img, use_ssim=use_ssim)
            verdict = classify(metrics, use_ssim=use_ssim)
            result = {
                "baseline": baseline_file,
                "current": current_file,
                "phase": phase,
                "verdict": verdict,
                **metrics,
            }
            if verdict == "FAIL":
                any_fail = True

        results.append(result)

    # JSON output to stdout
    print(json.dumps(results, indent=2))

    # Human-readable summary to stderr
    print("\n--- Screenshot Regression Report ---", file=sys.stderr)
    header = f"{'Phase':<30} {'Verdict':<6} {'MAD':<10} {'Changed%':<10} {'MaxDiff':<8}"
    if use_ssim:
        header += f" {'SSIM':<8}"
    print(header, file=sys.stderr)
    print("-" * (74 + (9 if use_ssim else 0)), file=sys.stderr)
    for r in results:
        phase = r.get("phase", "?")
        verdict = r.get("verdict", "?")
        mad = f"{r.get('mad', 0):.4f}" if "mad" in r else "N/A"
        pct = f"{r.get('changed_pct', 0):.1f}%" if "changed_pct" in r else "N/A"
        maxd = str(r.get("max_diff", "N/A"))
        reason = r.get("reason", "")
        line = f"{phase:<30} {verdict:<6} {mad:<10} {pct:<10} {maxd:<8}"
        if use_ssim:
            ssim_val = r.get("ssim")
            ssim_str = f"{ssim_val:.4f}" if ssim_val is not None else "N/A"
            line += f" {ssim_str:<8}"
        if reason:
            line += f" ({reason})"
        print(line, file=sys.stderr)

    pass_count = sum(1 for r in results if r["verdict"] == "PASS")
    warn_count = sum(1 for r in results if r["verdict"] == "WARN")
    fail_count = sum(1 for r in results if r["verdict"] == "FAIL")
    print(f"\nTotal: {pass_count} PASS, {warn_count} WARN, {fail_count} FAIL", file=sys.stderr)

    sys.exit(1 if any_fail else 0)


if __name__ == "__main__":
    main()
