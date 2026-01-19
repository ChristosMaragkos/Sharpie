namespace Sharpie.Core.Drivers;

public abstract class InputHandler
{
    public abstract (byte, byte) GetInputState();

    protected void AddKeyToState(ref byte controllerState, ControllerKeys key) =>
        controllerState |= (byte)key;

    protected bool IsButtonStateOn(byte controllerState, ControllerKeys button)
    {
        var buttonVal = (byte)button;
        var buttonOn = controllerState & buttonVal;
        return buttonOn == buttonVal;
    }

    [Flags]
    protected enum ControllerKeys : byte
    {
        Up = 0x01,
        Down = 0x02,
        Left = 0x04,
        Right = 0x08,
        ButtonA = 0x10,
        ButtonB = 0x20,
        ButtonStart = 0x40,
        ButtonOption = 0x80,
    }
}
