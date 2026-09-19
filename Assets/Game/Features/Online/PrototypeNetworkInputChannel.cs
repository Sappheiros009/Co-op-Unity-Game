using System;

namespace SlimeCoop.Prototype
{
    /// <summary>One approved connection's bounded intent inbox, shared by waiting-room and chapter movement.</summary>
    internal sealed class PrototypeNetworkInputChannel
    {
        public bool Connected = true;
        public long Sequence { get; private set; }
        private double _receivedAt = -1, _rateWindow = -1;
        private int _requests;
        private PrototypePlayerInput _frame = PrototypePlayerInput.Neutral();
        public bool Submit(long sequence, PrototypePlayerInput frame, double now, out string error, string stateError = "")
        {
            error = "";
            if (!Connected) error = "unknown_connection";
            else if (!double.IsFinite(now) || now < 0 || now < _receivedAt) error = "invalid_server_clock";
            if (error.Length > 0) return false;
            if (_rateWindow < 0 || now - _rateWindow >= 1) { _rateWindow = now; _requests = 0; }
            if (++_requests > PrototypeNetworkInputBuffer.RequestsPerSecond) error = "input_rate_limit";
            else if (!string.IsNullOrEmpty(stateError)) error = stateError;
            else if (sequence <= Sequence || sequence - Sequence > 1024) error = "invalid_sequence";
            else if (!frame.IsValid) error = "invalid_input";
            if (error.Length > 0) return false;
            if (frame.hasControl && _frame.hasControl && now - _receivedAt <= PrototypeNetworkInputBuffer.SilenceTimeoutSeconds)
            {
                frame.motor.jump |= _frame.motor.jump;
                frame.interaction.interactPressed |= _frame.interaction.interactPressed;
                frame.interaction.usePressed |= _frame.interaction.usePressed;
                frame.interaction.dropPressed |= _frame.interaction.dropPressed;
                if (frame.interaction.selectedSlot < 0) frame.interaction.selectedSlot = _frame.interaction.selectedSlot;
                frame.necklacePressed |= _frame.necklacePressed; frame.pingPressed |= _frame.pingPressed;
            }
            _frame = frame; Sequence = sequence; _receivedAt = now; return true;
        }
        public PrototypePlayerInput Consume(double now)
        {
            if (!Connected || _receivedAt < 0 || !double.IsFinite(now) || now < _receivedAt || now - _receivedAt > PrototypeNetworkInputBuffer.SilenceTimeoutSeconds)
                _frame = PrototypePlayerInput.Neutral(_frame.yaw, _frame.pitch);
            var frame = _frame; _frame.ClearEdges(); return frame;
        }
        public void Clear()
        { _frame = PrototypePlayerInput.Neutral(_frame.yaw, _frame.pitch); _receivedAt = -1; }
    }
}
