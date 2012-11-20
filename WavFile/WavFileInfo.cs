﻿/*
 * WavFile
 * Copyright © Wiesław Šoltés 2010-2012. All Rights Reserved
 */

namespace WavFile
{
    #region References

    using System;
    using System.Text;

    #endregion

    #region WavFileInfo

    public static class WavFileInfo
    {
        /// <summary>
        /// Read WAV file header
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static WavFileHeader ReadFileHeader(System.IO.FileStream f)
        {
            WavFileHeader h = new WavFileHeader();
            h.HeaderSize = 0;

            // read WAV header
            System.IO.BinaryReader b = new System.IO.BinaryReader(f);

            // WAVE
            h.ChunkID = b.ReadUInt32();         // 0x46464952, "RIFF"
            h.ChunkSize = b.ReadUInt32();       // 36 + SubChunk2Size, 4 + (8 + SubChunk1Size) + (8 + SubChunk2Size)
            h.Format = b.ReadUInt32();          // 0x45564157, "WAVE"

            h.HeaderSize += 12;

            // fmt
            h.Subchunk1ID = b.ReadUInt32();     // 0x20746d66, "fmt "
            h.Subchunk1Size = b.ReadUInt32();   // 16 for PCM, 40 for WAVEFORMATEXTENSIBLE
            h.AudioFormat = b.ReadUInt16();     // PCM = 1, WAVEFORMATEXTENSIBLE.SubFormat = 0xFFFE
            h.NumChannels = b.ReadUInt16();     // Mono = 1, Stereo = 2, etc.
            h.SampleRate = b.ReadUInt32();      // 8000, 44100, etc.
            h.ByteRate = b.ReadUInt32();        // SampleRate * NumChannels * BitsPerSample/8
            h.BlockAlign = b.ReadUInt16();      // NumChannels * BitsPerSample/8
            h.BitsPerSample = b.ReadUInt16();   // 8 bits = 8, 16 bits = 16, etc.

            h.HeaderSize += 24;

            // read PCM data or extensible data if exists
            if (h.Subchunk1Size == 16 && h.AudioFormat == 1) // PCM
            {
                h.IsExtensible = false;

                // Note: 8-bit samples are stored as unsigned bytes, ranging from 0 to 255. 16-bit samples are stored as 2's-complement signed integers, ranging from -32768 to 32767.
                // data
                h.Subchunk2ID = b.ReadUInt32();     // 0x61746164, "data"
                h.Subchunk2Size = b.ReadUInt32();   // NumSamples * NumChannels * BitsPerSample/8

                h.HeaderSize += 8;
            }
            else if (h.Subchunk1Size > 16 && h.AudioFormat == 0xFFFE) // WAVEFORMATEXTENSIBLE
            {
                // read WAVEFORMATEXTENSIBLE
                h.ExtraParamSize = b.ReadUInt16();
                h.HeaderSize += 2;

                if (h.ExtraParamSize == 22) // if cbSize is set to 22 => WAVEFORMATEXTENSIBLE
                {
                    h.IsExtensible = true;

                    //union {
                    //    WORD wValidBitsPerSample; // bits of precision
                    //    WORD wSamplesPerBlock;    // valid if wBitsPerSample==0
                    //    WORD wReserved;           // If neither applies, set to zero.
                    //} Samples;
                    h.Samples = b.ReadUInt16();

                    // DWORD dwChannelMask; which channels are present in stream
                    h.ChannelMask = b.ReadUInt32();

                    // GUID SubFormat
                    byte[] SubFormat = b.ReadBytes(16);

                    h.HeaderSize += 22;

                    // check sub-format
                    h.GuidSubFormat = new Guid(SubFormat);
                    if (h.GuidSubFormat != WavFileHeader.subTypePCM && h.GuidSubFormat != WavFileHeader.subTypeIEEE_FLOAT)
                    {
                        throw new Exception(String.Format("Not supported WAV file type: {0}", h.GuidSubFormat));
                    }

                    // find "data" chunk
                    while (b.PeekChar() != -1)
                    {
                        UInt32 chunk = b.ReadUInt32();
                        h.HeaderSize += 4;

                        if (chunk == 0x61746164) // "data" chunk
                        {
                            h.Subchunk2ID = chunk;              // 0x61746164, "data"
                            h.Subchunk2Size = b.ReadUInt32();   // NumSamples * NumChannels * BitsPerSample/8

                            h.HeaderSize += 4;

                            break;
                        }
                        else
                        {
                            // read other non "data" chunks
                            UInt32 chunkSize = b.ReadUInt32();

                            h.HeaderSize += 4;

                            string chunkName = Encoding.ASCII.GetString(BitConverter.GetBytes(chunk));
                            byte[] chunkData = b.ReadBytes((int)chunkSize);

                            h.HeaderSize += (int)chunkSize;
                        }
                    }
                }
                else
                {
                    throw new Exception("Not supported WAV file header.");
                }
            }
            else
            {
                throw new Exception("Not supported WAV file header.");
            }

            // calculate number of total samples
            h.TotalSamples = (long)((double)h.Subchunk2Size / ((double)h.NumChannels * (double)h.BitsPerSample / 8));

            // calculate dureation in seconds
            h.Duration = (1 / (double)h.SampleRate) * (double)h.TotalSamples;

            return h;
        }

        /// <summary>
        /// Write WAV file header
        /// </summary>
        /// <param name="f"></param>
        /// <param name="h"></param>
        public static void WriteFileHeader(System.IO.FileStream f, WavFileHeader h)
        {
            // write WAV header
            System.IO.BinaryWriter b = new System.IO.BinaryWriter(f);

            // WAVE
            b.Write((UInt32)0x46464952); // 0x46464952, "RIFF"
            b.Write(h.ChunkSize);
            b.Write((UInt32)0x45564157); // 0x45564157, "WAVE"

            // fmt
            b.Write((UInt32)0x20746d66); // 0x20746d66, "fmt "
            b.Write(h.Subchunk1Size);
            b.Write(h.AudioFormat);
            b.Write(h.NumChannels);
            b.Write(h.SampleRate);
            b.Write(h.ByteRate);
            b.Write(h.BlockAlign);
            b.Write(h.BitsPerSample);

            // write PCM data or extensible data if exists
            if (h.Subchunk1Size == 16 && h.AudioFormat == 1) // PCM
            {
                b.Write((UInt32)0x61746164); // 0x61746164, "data"
                b.Write(h.Subchunk2Size);
            }
            else if (h.Subchunk1Size > 16 && h.AudioFormat == 0xFFFE) // WAVEFORMATEXTENSIBLE
            {
                // write WAVEFORMATEXTENSIBLE
                b.Write(h.ExtraParamSize);

                b.Write(h.Samples);
                b.Write(h.ChannelMask);
                b.Write(h.GuidSubFormat.ToByteArray());

                b.Write((UInt32)0x61746164); // 0x61746164, "data"
                b.Write(h.Subchunk2Size);
            }
            else
            {
                throw new Exception("Not supported WAV file header.");
            }
        }

        /// <summary>
        /// Get mono WAV file header from multi-channel WAV file
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        public static WavFileHeader GetMonoWavFileHeader(WavFileHeader h)
        {
            // each mono output file has the same header
            WavFileHeader monoFileHeader = new WavFileHeader();

            // WAVE
            monoFileHeader.ChunkID = (UInt32)0x46464952; // 0x46464952, "RIFF"
            monoFileHeader.ChunkSize = 36 + (h.Subchunk2Size / h.NumChannels); // 36 + SubChunk2Size, 4 + (8 + SubChunk1Size) + (8 + SubChunk2Size)
            monoFileHeader.Format = (UInt32)0x45564157; // 0x45564157, "WAVE"

            // fmt
            monoFileHeader.Subchunk1ID = (UInt32)0x20746d66; // 0x20746d66, "fmt "
            monoFileHeader.Subchunk1Size = 16; // 16 for PCM, 40 for WAVEFORMATEXTENSIBLE
            monoFileHeader.AudioFormat = (UInt16)1; // PCM = 1, WAVEFORMATEXTENSIBLE.SubFormat = 0xFFFE
            monoFileHeader.NumChannels = (UInt16)1; // Mono = 1, Stereo = 2, etc.
            monoFileHeader.SampleRate = h.SampleRate; // 8000, 44100, etc.
            monoFileHeader.ByteRate = (UInt32)((h.SampleRate * 1 * h.BitsPerSample) / 8); // SampleRate * NumChannels * BitsPerSample/8
            monoFileHeader.BlockAlign = (UInt16)((1 * h.BitsPerSample) / 8); // NumChannels * BitsPerSample/8
            monoFileHeader.BitsPerSample = h.BitsPerSample; // 8 bits = 8, 16 bits = 16, etc.

            // extensible
            monoFileHeader.ExtraParamSize = (UInt16)0;
            monoFileHeader.ChannelMask = (UInt32)0;
            monoFileHeader.GuidSubFormat = new Guid();

            // data
            monoFileHeader.Subchunk2ID = (UInt32)0x61746164; // 0x61746164, "data"
            monoFileHeader.Subchunk2Size = (h.Subchunk2Size / h.NumChannels); // NumSamples * NumChannels * BitsPerSample/8

            // info
            monoFileHeader.IsExtensible = false;
            monoFileHeader.HeaderSize = 44;
            monoFileHeader.TotalSamples = (long)((double)monoFileHeader.Subchunk2Size / ((double)monoFileHeader.NumChannels * (double)monoFileHeader.BitsPerSample / 8));
            monoFileHeader.Duration = (1 / (double)monoFileHeader.SampleRate) * (double)monoFileHeader.TotalSamples;

            return monoFileHeader;
        }


    }

    #endregion
}