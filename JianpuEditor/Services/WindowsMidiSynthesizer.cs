using System;
using System.Runtime.InteropServices;

namespace JianpuEditor.Services
{
    internal sealed class WindowsMidiSynthesizer : IDisposable
    {
        private const int MidiMapper = -1;
        private const int CallbackNull = 0;
        private const int MmsyserrNoerror = 0;

        private IntPtr _handle = IntPtr.Zero;
        private int _deviceId = MidiMapper;
        private bool _disposed;
        private string _deviceName = "未打开";

        public void NoteOn(int channel, int note, int velocity)
        {
            SendShortMessage("NoteOn", 0x90 | (channel & 0x0F), note, velocity);
        }

        public void NoteOff(int channel, int note)
        {
            SendShortMessage("NoteOff", 0x80 | (channel & 0x0F), note, 0);
        }

        public void AllNotesOff()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            for (var channel = 0; channel < 16; channel++)
            {
                SendShortMessage("AllNotesOff", 0xB0 | channel, 123, 0);
            }
        }

        private void EnsureOpen()
        {
            if (_handle != IntPtr.Zero)
            {
                return;
            }

            AppLog.Info("正在打开 MIDI 输出设备...");
            var lastError = MmsyserrNoerror;
            if (TryOpenDevice(MidiMapper, "MIDI Mapper", out lastError))
            {
                return;
            }

            var deviceCount = midiOutGetNumDevs();
            AppLog.Info("MIDI Mapper 打开失败，错误码=" + lastError + "，本机设备数=" + deviceCount);
            for (var deviceId = 0; deviceId < deviceCount; deviceId++)
            {
                var name = GetDeviceName(deviceId);
                if (TryOpenDevice(deviceId, name, out lastError))
                {
                    return;
                }

                AppLog.Error("打开 MIDI 设备失败: id=" + deviceId + ", name=" + name + ", error=" + lastError);
            }

            throw new InvalidOperationException(
                "无法打开 MIDI 输出设备。错误码=" + lastError +
                "。请确认系统已启用 MIDI 合成器（如 Microsoft GS Wavetable Synth）。" +
                " 日志: " + AppLog.LogFilePath);
        }

        private bool TryOpenDevice(int deviceId, string deviceLabel, out int errorCode)
        {
            errorCode = midiOutOpen(out var handle, deviceId, IntPtr.Zero, IntPtr.Zero, CallbackNull);
            if (errorCode != MmsyserrNoerror)
            {
                return false;
            }

            _handle = handle;
            _deviceId = deviceId;
            _deviceName = deviceLabel;
            AppLog.Info("MIDI 输出设备已打开: id=" + deviceId + ", name=" + deviceLabel);
            return true;
        }

        private static string GetDeviceName(int deviceId)
        {
            var caps = new MidiOutCaps();
            var size = Marshal.SizeOf(caps);
            var result = midiOutGetDevCaps(deviceId, ref caps, size);
            if (result != MmsyserrNoerror)
            {
                return "Device " + deviceId;
            }

            return (caps.szPname ?? string.Empty).Trim();
        }

        private void SendShortMessage(string action, int status, int data1, int data2)
        {
            if (_disposed)
            {
                return;
            }

            EnsureOpen();
            var message = status | ((data1 & 0x7F) << 8) | ((data2 & 0x7F) << 16);
            var result = midiOutShortMsg(_handle, message);
            if (result != MmsyserrNoerror)
            {
                var text = string.Format(
                    "MIDI 发送失败: action={0}, device={1}, status=0x{2:X2}, note={3}, velocity={4}, error={5}",
                    action,
                    _deviceName,
                    status,
                    data1,
                    data2,
                    result);
                AppLog.Error(text);
                throw new InvalidOperationException(text + "。日志: " + AppLog.LogFilePath);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_handle != IntPtr.Zero)
            {
                try
                {
                    AllNotesOff();
                }
                catch (Exception ex)
                {
                    AppLog.Exception("关闭 MIDI 前发送 AllNotesOff 失败", ex);
                }

                var result = midiOutClose(_handle);
                if (result != MmsyserrNoerror)
                {
                    AppLog.Error("关闭 MIDI 设备失败: error=" + result + ", device=" + _deviceName);
                }
                else
                {
                    AppLog.Info("MIDI 输出设备已关闭: " + _deviceName);
                }

                _handle = IntPtr.Zero;
            }
        }

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int midiOutGetDevCaps(int uDeviceID, ref MidiOutCaps lpCaps, int uSize);

        [DllImport("winmm.dll")]
        private static extern int midiOutGetNumDevs();

        [DllImport("winmm.dll")]
        private static extern int midiOutOpen(
            out IntPtr lphMidiOut,
            int uDeviceID,
            IntPtr dwCallback,
            IntPtr dwInstance,
            int dwFlags);

        [DllImport("winmm.dll")]
        private static extern int midiOutShortMsg(IntPtr hMidiOut, int dwMsg);

        [DllImport("winmm.dll")]
        private static extern int midiOutClose(IntPtr hMidiOut);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MidiOutCaps
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;

            public ushort wTechnology;
            public ushort wVoices;
            public ushort wNotes;
            public ushort wChannelMask;
            public uint dwSupport;
        }
    }
}
