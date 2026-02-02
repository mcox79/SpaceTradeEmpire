#nullable enable

using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Commands;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Node
{
	[Signal] public delegate void SimLoadedEventHandler();

	[Export] public int WorldSeed { get; set; } = 12345;
	[Export] public int StarCount { get; set; } = 20;

	// Sim loop timing. If 0, runs as fast as possible.
	[Export] public int TickDelayMs { get; set; } = 100;

	// If true, deletes the quicksave on boot (runtime only).
	[Export] public bool ResetSaveOnBoot { get; set; } = true;

	private SimKernel _kernel = null!;
	private CancellationTokenSource? _cts;
	private Task? _simTask;

	private readonly ReaderWriterLockSlim _stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

	private string _savePathAbs = "";
	private volatile bool _saveRequested = false;
	private volatile bool _loadRequested = false;

	private int _isLoading = 0;
	private volatile bool _emitLoadCompletePending = false;

	public bool IsLoading
	{
		get => Volatile.Read(ref _isLoading) != 0;
		private set => Volatile.Write(ref _isLoading, value ? 1 : 0);
	}

	public override void _Ready()
	{
		// Autoloads run during editor startup. Do not execute runtime logic in the editor.
		if (Engine.IsEditorHint())
		{
			return;
		}

		Input.MouseMode = Input.MouseModeEnum.Visible;

		// Compute save path once on main thread. Do NOT call Godot API from worker thread.
		_savePathAbs = ProjectSettings.GlobalizePath("user://quicksave.json");

		if (ResetSaveOnBoot && File.Exists(_savePathAbs))
		{
			try { File.Delete(_savePathAbs); }
			catch (Exception ex) { GD.PrintErr(ex.ToString()); }
		}

		InitializeKernel();
		StartSimulation();
	}

	public override void _ExitTree()
	{
		StopSimulation();
		_stateLock.Dispose();
	}

	public override void _Process(double delta)
	{
		// Emit load complete on main thread.
		if (_emitLoadCompletePending)
		{
			_emitLoadCompletePending = false;
			EmitSignal(SignalName.SimLoaded);
		}
	}

	private void InitializeKernel()
	{
		GD.Print("[BRIDGE] Initializing SimCore Kernel...");
		_kernel = new SimKernel(WorldSeed);

		if (File.Exists(_savePathAbs) && !ResetSaveOnBoot)
		{
			RequestLoad();
		}
		else
		{
			_stateLock.EnterWriteLock();
			try
			{
				GalaxyGenerator.Generate(_kernel.State, StarCount, 200f);
			}
			finally
			{
				_stateLock.ExitWriteLock();
			}
		}
	}

	private void StartSimulation()
	{
		if (_simTask != null && !_simTask.IsCompleted) return;

		_cts = new CancellationTokenSource();
		_simTask = Task.Run(() => SimLoop(_cts.Token), _cts.Token);

		GD.Print("[BRIDGE] Simulation Thread Started.");
	}

	private void StopSimulation()
	{
		try
		{
			if (_cts != null && !_cts.IsCancellationRequested)
			{
				_cts.Cancel();
			}

			// Best-effort join.
			if (_simTask != null && !_simTask.IsCompleted)
			{
				_simTask.Wait(1000);
			}
		}
		catch
		{
			// Suppress shutdown exceptions.
		}
		finally
		{
			_cts?.Dispose();
			_cts = null;
			_simTask = null;
		}
	}

	private async Task SimLoop(CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			try
			{
				// Loading is exclusive.
				if (_loadRequested)
				{
					_loadRequested = false;
					ExecuteLoad();
				}

				// Saving can occur between steps.
				if (_saveRequested)
				{
					_saveRequested = false;
					ExecuteSave();
				}

				_stateLock.EnterWriteLock();
				try
				{
					_kernel.Step();
				}
				finally
				{
					_stateLock.ExitWriteLock();
				}

				if (TickDelayMs > 0)
				{
					await Task.Delay(TickDelayMs, token);
				}
				else
				{
					await Task.Yield();
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[BRIDGE] CRITICAL SIM ERROR: {ex}");
				await Task.Delay(250, token);
			}
		}
	}

	// --- PUBLIC API (Thread-Safe) ---

	public void ExecuteSafeRead(Action<SimState> action)
	{
		if (IsLoading) return;

		_stateLock.EnterReadLock();
		try
		{
			action(_kernel.State);
		}
		finally
		{
			_stateLock.ExitReadLock();
		}
	}

	public void EnqueueCommand(ICommand cmd)
	{
		if (IsLoading) return;

		_stateLock.EnterWriteLock();
		try
		{
			_kernel.EnqueueCommand(cmd);
		}
		finally
		{
			_stateLock.ExitWriteLock();
		}
	}

	// GDScript-friendly snapshot accessor
	public Godot.Collections.Dictionary GetPlayerSnapshot()
	{
		var dict = new Godot.Collections.Dictionary();
		if (IsLoading) return dict;

		_stateLock.EnterReadLock();
		try
		{
			dict["credits"] = _kernel.State.PlayerCredits;

			var cargo = new Godot.Collections.Dictionary();
			foreach (var kv in _kernel.State.PlayerCargo)
			{
				cargo[kv.Key] = kv.Value;
			}
			dict["cargo"] = cargo;

			return dict;
		}
		finally
		{
			_stateLock.ExitReadLock();
		}
	}

	public void RequestSave()
	{
		_saveRequested = true;
	}

	public void RequestLoad()
	{
		_loadRequested = true;
	}

	private void ExecuteSave()
	{
		try
		{
			_stateLock.EnterReadLock();
			string json;
			try
			{
				json = _kernel.SaveToString();
			}
			finally
			{
				_stateLock.ExitReadLock();
			}

			File.WriteAllText(_savePathAbs, json);
			GD.Print($"[BRIDGE] Saved: {_savePathAbs}");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[BRIDGE] Save failed: {ex}");
		}
	}

	private void ExecuteLoad()
	{
		if (!File.Exists(_savePathAbs))
		{
			GD.Print("[BRIDGE] Load requested but no save exists.");
			return;
		}

		IsLoading = true;
		try
		{
			var json = File.ReadAllText(_savePathAbs);

			_stateLock.EnterWriteLock();
			try
			{
				_kernel.LoadFromString(json);
			}
			finally
			{
				_stateLock.ExitWriteLock();
			}

			_emitLoadCompletePending = true;
			GD.Print("[BRIDGE] Load complete.");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[BRIDGE] Load failed: {ex}");
		}
		finally
		{
			IsLoading = false;
		}
	}
}
