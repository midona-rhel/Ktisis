using System;

using Dalamud.Logging;
using Dalamud.Hooking;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;

using Ktisis.Events;
using Ktisis.Structs.Input;
using Ktisis.Overlay;

namespace Ktisis.Interop.Hooks {
	internal static class ControlHooks {
		public static KeyboardState KeyboardState = new();

		internal unsafe delegate void InputDelegate(InputEvent* keyState, IntPtr a2, IntPtr a3, MouseState* mouseState, IntPtr a5);
		internal static Hook<InputDelegate> InputHook = null!;

		internal unsafe static void InputDetour(InputEvent* keyState, IntPtr a2, IntPtr a3, MouseState* mouseState, IntPtr a5) {
			try {
				if (mouseState != null) {
					// TODO
				}

				if (keyState != null) {
					var keys = keyState->Keyboard->GetQueue();
					for (var i = 0; i < keys->QueueCount; i++) {
						var k = keys->Queue[i];

						// TODO: Input event manager
						if (k->Event == KeyEvent.Pressed && k->KeyCode == 27) {
							if (OverlayWindow.GizmoOwner != null) {
								OverlayWindow.DeselectGizmo();
								k->Event = KeyEvent.None;
								keyState->Keyboard->ClearQueue();
							}
						}
					}
				}
			} catch (Exception e) {
				PluginLog.Error(e, "Error in InputDetour.");
			}
		}

		// This function is pretty powerful. We only need it for the release event though; InputHook can't pick it up if an input gets blocked.
		// That said, this one can't reliably distinguish between a button being pressed or held, or give us the input state, so we need to use both.

		internal unsafe delegate IntPtr InputDelegate2(ulong a1, uint a2, ulong a3, uint a4);
		internal static Hook<InputDelegate2> InputHook2 = null!;

		internal unsafe static IntPtr InputDetour2(ulong a1, uint a2, ulong a3, uint a4) {
			var exec = InputHook2.Original(a1, a2, a3, a4);

			if (Ktisis.IsInGPose && a2 == 257) { // Released
				if (EventManager.OnKeyReleased != null)
					EventManager.OnKeyReleased((VirtualKey)a3);
			}

			return exec;
		}

		// Init & dispose

		internal static void Init() {
			unsafe {
				var addr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 83 7B 58 00");
				InputHook = Hook<InputDelegate>.FromAddress(addr, InputDetour);
				InputHook.Enable();

				var addr2 = Services.SigScanner.ScanText("48 89 5C 24 ?? 55 56 57 41 56 41 57 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 40 4D 8B F9");
				InputHook2 = Hook<InputDelegate2>.FromAddress(addr2, InputDetour2);
				InputHook2.Enable();
			}
		}

		internal static void Dispose() {
			InputHook.Disable();
			InputHook.Dispose();

			InputHook2.Disable();
			InputHook2.Dispose();
		}
	}
}