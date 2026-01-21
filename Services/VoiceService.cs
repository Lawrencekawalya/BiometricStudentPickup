using System.Speech.Synthesis;
using System.Diagnostics;
using System;

namespace BiometricStudentPickup.Services
{
    public class VoiceService
    {
        private readonly SpeechSynthesizer _synth;

        public VoiceService()
        {
            try
            {
                Debug.WriteLine("Creating SpeechSynthesizer...");
                _synth = new SpeechSynthesizer();

                // Select David Desktop (confirmed to exist)
                _synth.SelectVoice("Microsoft David Desktop");
                Debug.WriteLine("Selected Microsoft David Desktop");

                _synth.SetOutputToDefaultAudioDevice();
                _synth.Volume = 100;
                _synth.Rate = 0;

                Debug.WriteLine("VoiceService initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VoiceService ERROR: {ex.Message}");
                throw; // Re-throw to see the error
            }
        }

        public void Speak(string message)
        {
            Debug.WriteLine($"VoiceService.Speak: {message}");

            try
            {
                // Cancel any ongoing speech
                if (_synth.State == SynthesizerState.Speaking)
                {
                    _synth.SpeakAsyncCancelAll();
                }

                // Use SpeakAsync (non-blocking)
                _synth.SpeakAsync(message);
                Debug.WriteLine("Speech started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Speak ERROR: {ex.Message}");
            }
        }
    }
}