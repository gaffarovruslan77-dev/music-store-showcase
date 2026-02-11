using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Bogus;

namespace MusicStoreShowcase.Services
{
    public class MusicGeneratorService
    {
        private const int SampleRate = 44100;
        private const int DurationSeconds = 15;
        
        public byte[] GenerateMusic(int seed)
        {
            var faker = new Faker { Random = new Randomizer(seed) };
            
            // Select tempo and key
            var tempo = faker.Random.Int(90, 140); // BPM
            var beatDuration = 60.0 / tempo;
            var rootNote = faker.Random.Int(48, 60); // C3 to C4
            
            // Generate chord progression using common patterns
            var chords = GenerateChordProgression(faker);
            
            // Create melody, bass, and simple percussion with LOUDER volumes
            var melody = GenerateMelody(faker, chords, rootNote, beatDuration);
            var bass = GenerateBass(faker, chords, rootNote, beatDuration);
            var hihat = GenerateHiHat(faker, beatDuration);
            
            // Mix all tracks with proper levels
            var mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1));
            mixer.AddMixerInput(melody);
            mixer.AddMixerInput(bass);
            mixer.AddMixerInput(hihat);
            
            // Add simple reverb effect
            var withReverb = AddSimpleReverb(mixer);
            
            // INCREASE OVERALL VOLUME by 2.5x
            var amplified = new VolumeSampleProvider(withReverb)
            {
                Volume = 2.5f
            };
            
            // Convert to WAV
            using var ms = new MemoryStream();
            WaveFileWriter.WriteWavFileToStream(ms, amplified.ToWaveProvider());
            return ms.ToArray();
        }
        
        private int[] GenerateChordProgression(Faker faker)
        {
            // Common chord progressions in popular music
            var progressions = new[]
            {
                new[] { 0, 5, 7, 5 },      // I-V-vi-V (very common in pop)
                new[] { 0, 7, 5, 5 },      // I-vi-V-V
                new[] { 0, 5, 3, 7 },      // I-V-IV-vi
                new[] { 0, 3, 7, 5 },      // I-IV-vi-V
                new[] { 7, 5, 0, 0 }       // vi-V-I-I
            };
            
            return faker.PickRandom(progressions);
        }
        
        private ISampleProvider GenerateMelody(Faker faker, int[] chords, int rootNote, double beatDuration)
        {
            var samples = new List<ISampleProvider>();
            
            // Pentatonic scale intervals (sounds musical)
            var pentatonic = new[] { 0, 2, 4, 7, 9, 12 };
            
            double currentTime = 0;
            
            // Generate 4 bars, each with 4 beats
            for (int bar = 0; bar < 4; bar++)
            {
                var chordRoot = chords[bar % chords.Length];
                
                for (int beat = 0; beat < 4; beat++)
                {
                    // 70% chance to play a note
                    if (faker.Random.Bool(0.7f))
                    {
                        var scaleNote = faker.PickRandom(pentatonic);
                        var midiNote = rootNote + chordRoot + scaleNote;
                        var frequency = MidiNoteToFrequency(midiNote);
                        
                        // Create a note with ADSR envelope - LOUDER
                        var duration = beatDuration * faker.Random.Double(0.3, 0.9);
                        var signal = CreateNoteWithADSR(frequency, duration, 0.25); // was 0.12
                        
                        samples.Add(new OffsetSampleProvider(signal)
                        {
                            DelayBy = TimeSpan.FromSeconds(currentTime)
                        });
                    }
                    
                    currentTime += beatDuration;
                }
            }
            
            return new MixingSampleProvider(samples);
        }
        
        private ISampleProvider GenerateBass(Faker faker, int[] chords, int rootNote, double beatDuration)
        {
            var samples = new List<ISampleProvider>();
            var bassRoot = rootNote - 24; // Two octaves lower
            
            double currentTime = 0;
            
            // Bass plays on every beat
            for (int bar = 0; bar < 4; bar++)
            {
                var chordRoot = chords[bar % chords.Length];
                
                for (int beat = 0; beat < 4; beat++)
                {
                    var midiNote = bassRoot + chordRoot;
                    var frequency = MidiNoteToFrequency(midiNote);
                    
                    // Bass note with shorter duration and ADSR - LOUDER
                    var signal = CreateNoteWithADSR(frequency, beatDuration * 0.6, 0.35); // was 0.18
                    
                    samples.Add(new OffsetSampleProvider(signal)
                    {
                        DelayBy = TimeSpan.FromSeconds(currentTime)
                    });
                    
                    currentTime += beatDuration;
                }
            }
            
            return new MixingSampleProvider(samples);
        }
        
        private ISampleProvider GenerateHiHat(Faker faker, double beatDuration)
        {
            var samples = new List<ISampleProvider>();
            
            double currentTime = 0;
            
            // Hi-hat on every half beat
            for (int i = 0; i < 32; i++)
            {
                // Simple noise burst for hi-hat - LOUDER
                var hihat = CreateHiHat(beatDuration * 0.15, 0.10); // was 0.04
                
                samples.Add(new OffsetSampleProvider(hihat)
                {
                    DelayBy = TimeSpan.FromSeconds(currentTime)
                });
                
                currentTime += beatDuration / 2;
            }
            
            return new MixingSampleProvider(samples);
        }
        
        private ISampleProvider CreateNoteWithADSR(double frequency, double duration, double volume)
        {
            var totalSamples = (int)(duration * SampleRate);
            var samples = new float[totalSamples];
            
            // ADSR envelope parameters (in samples)
            var attackSamples = Math.Min(totalSamples / 20, 1000);      
            var decaySamples = Math.Min(totalSamples / 10, 2000);       
            var sustainLevel = 0.7;                                      
            var releaseSamples = Math.Min(totalSamples / 8, 3000);      
            
            var sustainSamples = totalSamples - attackSamples - decaySamples - releaseSamples;
            if (sustainSamples < 0)
            {
                attackSamples = totalSamples / 3;
                decaySamples = 0;
                sustainSamples = 0;
                releaseSamples = totalSamples - attackSamples;
            }
            
            for (int i = 0; i < totalSamples; i++)
            {
                // Generate sine wave
                double angle = 2.0 * Math.PI * frequency * i / SampleRate;
                float sineValue = (float)Math.Sin(angle);
                
                // Apply ADSR envelope
                float envelope = 0;
                
                if (i < attackSamples)
                {
                    envelope = (float)i / attackSamples;
                }
                else if (i < attackSamples + decaySamples)
                {
                    var decayProgress = (float)(i - attackSamples) / decaySamples;
                    envelope = 1.0f - (1.0f - (float)sustainLevel) * decayProgress;
                }
                else if (i < attackSamples + decaySamples + sustainSamples)
                {
                    envelope = (float)sustainLevel;
                }
                else
                {
                    var releaseProgress = (float)(i - attackSamples - decaySamples - sustainSamples) / releaseSamples;
                    envelope = (float)sustainLevel * (1.0f - releaseProgress);
                }
                
                samples[i] = sineValue * envelope * (float)volume;
            }
            
            return new CustomSampleProvider(samples, SampleRate);
        }
        
        private ISampleProvider CreateHiHat(double duration, double volume)
        {
            var random = new Random();
            var totalSamples = (int)(duration * SampleRate);
            var samples = new float[totalSamples];
            
            for (int i = 0; i < totalSamples; i++)
            {
                samples[i] = (float)((random.NextDouble() * 2 - 1) * volume);
                
                // Exponential fade out envelope
                float envelope = (float)Math.Exp(-5.0 * i / totalSamples);
                samples[i] *= envelope;
            }
            
            return new CustomSampleProvider(samples, SampleRate);
        }
        
        private ISampleProvider AddSimpleReverb(ISampleProvider input)
        {
            // Simple reverb using delay lines
            var delayTimes = new[] { 0.029, 0.037, 0.041, 0.043 }; // seconds
            var mixLevel = 0.20f; // 20% wet signal (was 15%)
            
            var delays = delayTimes.Select(dt => 
                new SimpleDelay(input, (int)(dt * SampleRate), 0.5f)
            ).ToList();
            
            var mixer = new MixingSampleProvider(input.WaveFormat);
            mixer.AddMixerInput(input);
            
            foreach (var delay in delays)
            {
                mixer.AddMixerInput(new VolumeSampleProvider(delay) { Volume = mixLevel });
            }
            
            return mixer;
        }
        
        private double MidiNoteToFrequency(int midiNote)
        {
            return 440.0 * Math.Pow(2.0, (midiNote - 69) / 12.0);
        }
    }
    
    // Helper class for custom samples
    public class CustomSampleProvider : ISampleProvider
    {
        private readonly float[] samples;
        private int position;
        
        public CustomSampleProvider(float[] samples, int sampleRate)
        {
            this.samples = samples;
            this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        }
        
        public WaveFormat WaveFormat { get; }
        
        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = 0;
            while (samplesRead < count && position < samples.Length)
            {
                buffer[offset + samplesRead] = samples[position];
                position++;
                samplesRead++;
            }
            return samplesRead;
        }
    }
    
    // Simple delay effect for reverb
    public class SimpleDelay : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float[] delayBuffer;
        private readonly float feedback;
        private int writePosition;
        
        public SimpleDelay(ISampleProvider source, int delaySamples, float feedback)
        {
            this.source = source;
            this.delayBuffer = new float[delaySamples];
            this.feedback = feedback;
            this.WaveFormat = source.WaveFormat;
        }
        
        public WaveFormat WaveFormat { get; }
        
        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);
            
            for (int i = 0; i < samplesRead; i++)
            {
                var delayed = delayBuffer[writePosition];
                delayBuffer[writePosition] = buffer[offset + i] + delayed * feedback;
                buffer[offset + i] = delayed;
                
                writePosition = (writePosition + 1) % delayBuffer.Length;
            }
            
            return samplesRead;
        }
    }
}
