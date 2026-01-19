using Raylib_cs;
using Sharpie.Core.Drivers;

namespace Sharpie.Runner.RaylibCs.Impl;

public class RaylibInputHandler : InputHandler
{
    public override (byte, byte) GetInputState()
    {
        byte controller1 = 0;
        byte controller2 = 0;

        controller1 = GetKeyboardState();

        if (Raylib.IsGamepadAvailable(1))
            controller1 |= GetGamepadState(1);

        if (Raylib.IsGamepadAvailable(0))
            controller2 = GetGamepadState(0);

        return (controller1, controller2);
    }

    private byte GetKeyboardState()
    {
        byte state = 0;

        if (Raylib.IsKeyDown(KeyboardKey.Up))
            AddKeyToState(ref state, ControllerKeys.Up);
        if (Raylib.IsKeyDown(KeyboardKey.Down))
            AddKeyToState(ref state, ControllerKeys.Down);
        if (Raylib.IsKeyDown(KeyboardKey.Left))
            AddKeyToState(ref state, ControllerKeys.Left);
        if (Raylib.IsKeyDown(KeyboardKey.Right))
            AddKeyToState(ref state, ControllerKeys.Right);

        if (Raylib.IsKeyDown(KeyboardKey.Z))
            AddKeyToState(ref state, ControllerKeys.ButtonA);
        if (Raylib.IsKeyDown(KeyboardKey.X))
            AddKeyToState(ref state, ControllerKeys.ButtonB);

        if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
            AddKeyToState(ref state, ControllerKeys.ButtonStart);
        if (Raylib.IsKeyDown(KeyboardKey.Tab))
            AddKeyToState(ref state, ControllerKeys.ButtonOption);

        return state;
    }

    private byte GetGamepadState(int gamepadIndex)
    {
        byte state = 0;

        if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.LeftFaceUp))
            AddKeyToState(ref state, ControllerKeys.Up);
        if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.LeftFaceDown))
            AddKeyToState(ref state, ControllerKeys.Down);
        if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.LeftFaceLeft))
            AddKeyToState(ref state, ControllerKeys.Left);
        if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.LeftFaceRight))
            AddKeyToState(ref state, ControllerKeys.Right);

        ApplyGamepadAxis(ref state, gamepadIndex);

        if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.RightFaceRight))
            AddKeyToState(ref state, ControllerKeys.ButtonA);
        if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.RightFaceDown))
            AddKeyToState(ref state, ControllerKeys.ButtonB);

        if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.MiddleRight))
            AddKeyToState(ref state, ControllerKeys.ButtonStart);
        if (Raylib.IsGamepadButtonDown(gamepadIndex, GamepadButton.MiddleLeft))
            AddKeyToState(ref state, ControllerKeys.ButtonOption);

        return state;
    }

    private void ApplyGamepadAxis(ref byte state, int gamepadIndex)
    {
        const float deadzone = 0.5f;

        var axisX = Raylib.GetGamepadAxisMovement(gamepadIndex, GamepadAxis.LeftX);
        var axisY = Raylib.GetGamepadAxisMovement(gamepadIndex, GamepadAxis.LeftY);

        if (axisX < -deadzone)
            AddKeyToState(ref state, ControllerKeys.Left);
        if (axisX > deadzone)
            AddKeyToState(ref state, ControllerKeys.Right);

        if (axisY < -deadzone)
            AddKeyToState(ref state, ControllerKeys.Up);
        if (axisY > deadzone)
            AddKeyToState(ref state, ControllerKeys.Down);
    }
}
