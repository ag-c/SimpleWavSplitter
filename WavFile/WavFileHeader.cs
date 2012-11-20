﻿/*
 * WavFile
 * Copyright © Wiesław Šoltés 2010-2012. All Rights Reserved
 */

namespace WavFile
{
    #region References

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;

    #endregion

    #region WavFileHeader

    /// <summary>
    /// The canonical WAVE format starts with the RIFF header
    /// Based on description from:
    /// https://ccrma.stanford.edu/courses/422/projects/WaveFormat/
    /// </summary>
    public struct WavFileHeader
    {
        //
        // WAVE                             bytes=12
        //
        public UInt32 ChunkID;          //  bytes=4
        public UInt32 ChunkSize;        //  bytes=4
        public UInt32 Format;           //  bytes=4
        //
        // fmt                              bytes=24
        //
        public UInt32 Subchunk1ID;      //  bytes=4
        public UInt32 Subchunk1Size;    //  bytes=4
        public UInt16 AudioFormat;      //  bytes=2
        public UInt16 NumChannels;      //  bytes=2
        public UInt32 SampleRate;       //  bytes=4
        public UInt32 ByteRate;         //  bytes=4
        public UInt16 BlockAlign;       //  bytes=2
        public UInt16 BitsPerSample;    //  bytes=2
        //
        // extra                            bytes=2
        // if h.Subchunk1Size > 16
        //
        public UInt16 ExtraParamSize;   //  bytes=2
        //
        // extensible                       bytes=22
        //
        public UInt16 Samples;          //  bytes=2
        public UInt32 ChannelMask;      //  bytes=4
        public Guid GuidSubFormat;      //  bytes=16
        //
        // data                             bytes=8
        //
        public UInt32 Subchunk2ID;      //  bytes=4
        public UInt32 Subchunk2Size;    //  bytes=4
        //
        // info
        //
        public bool IsExtensible;
        public int HeaderSize;  // normal WAV = 44 bytes, extensible WAV = 44 + 24 = 68 bytes (without extra chunks)
        public double Duration; // duration in seconds
        public long TotalSamples;
        //
        // channel types
        //
        public static readonly IList<WavChannel> WavChannelTypes = new ReadOnlyCollection<WavChannel>
            (
            new[] 
            {
                 new WavChannel ("Mono",                    "M",   0                                                ),
                 new WavChannel ("Left",                    "L",   0                                                ),
                 new WavChannel ("Right",                   "R",   0                                                )
            });

        //
        // multi-channel types
        //
        public static readonly IList<WavChannel> WavMultiChannelTypes = new ReadOnlyCollection<WavChannel>
            (
            new[] 
            {
                 new WavChannel ("Front Left",              "FL",   WavChannelMask.SPEAKER_FRONT_LEFT               ),
                 new WavChannel ("Front Right",             "FR",   WavChannelMask.SPEAKER_FRONT_RIGHT              ),
                 new WavChannel ("Front Center",            "FC",   WavChannelMask.SPEAKER_FRONT_CENTER             ),
                 new WavChannel ("Low Frequency",           "LF",   WavChannelMask.SPEAKER_LOW_FREQUENCY            ),
                 new WavChannel ("Back Left",               "BL",   WavChannelMask.SPEAKER_BACK_LEFT                ),
                 new WavChannel ("Back Right",              "BR",   WavChannelMask.SPEAKER_BACK_RIGHT               ),
                 new WavChannel ("Front Left of Center",    "FLC",  WavChannelMask.SPEAKER_FRONT_LEFT_OF_CENTER     ),
                 new WavChannel ("Front Right of Center",   "FRC",  WavChannelMask.SPEAKER_FRONT_RIGHT_OF_CENTER    ),
                 new WavChannel ("Back Center",             "BC",   WavChannelMask.SPEAKER_BACK_CENTER              ),
                 new WavChannel ("Side Left",               "SL",   WavChannelMask.SPEAKER_SIDE_LEFT                ),
                 new WavChannel ("Side Right",              "SR",   WavChannelMask.SPEAKER_SIDE_RIGHT               ),
                 new WavChannel ("Top Center",              "TC",   WavChannelMask.SPEAKER_TOP_CENTER               ),
                 new WavChannel ("Top Front Left",          "TFL",  WavChannelMask.SPEAKER_TOP_FRONT_LEFT           ),
                 new WavChannel ("Top Front Center",        "TFC",  WavChannelMask.SPEAKER_TOP_FRONT_CENTER         ),
                 new WavChannel ("Top Front Right",         "TFR",  WavChannelMask.SPEAKER_TOP_FRONT_RIGHT          ),
                 new WavChannel ("Top Back Left",           "TBL",  WavChannelMask.SPEAKER_TOP_BACK_LEFT            ),
                 new WavChannel ("Top Back Center",         "TBC",  WavChannelMask.SPEAKER_TOP_BACK_CENTER          ),
                 new WavChannel ("Top Back Right",          "TBR",  WavChannelMask.SPEAKER_TOP_BACK_RIGHT           )
            });

        // WAVEFORMATEXTENSIBLE sub-formats
        public static readonly Guid subTypePCM = new Guid("00000001-0000-0010-8000-00aa00389b71");
        public static readonly Guid subTypeIEEE_FLOAT = new Guid("00000003-0000-0010-8000-00aa00389b71");

        // ToString()
        public override string ToString()
        {
            return String.Format(
                "[WAVE]\n" +
                "ChunkID:\t\t{0}\n" +
                "ChunkSize:\t{1}\n" +
                "Format:\t\t{2}\n" +
                "[fmt]\n" +
                "Subchunk1ID:\t{3}\n" +
                "Subchunk1Size:\t{4}\n" +
                "AudioFormat:\t{5}\n" +
                "NumChannels:\t{6}\n" +
                "SampleRate:\t{7}\n" +
                "ByteRate:\t\t{8}\n" +
                "BlockAlign:\t{9}\n" +
                "BitsPerSample:\t{10}\n" +
                "[extra]\n" +
                "ExtraParamSize:\t{11}\n" +
                "[extensible]\n" +
                "Samples:\t\t{12}\n" +
                "ChannelMask:\t{13}\n" +
                "GuidSubFormat:\t{14}\n" +
                "[data]\n" +
                "Subchunk2ID:\t{15}\n" +
                "Subchunk2Size:\t{16}\n" +
                "[info]\n" +
                "IsExtensible:\t{17}\n" +
                "HeaderSize:\t{18}\n" +
                "Duration:\t\t{19}\n" +
                "TotalSamples:\t{20}",
                Encoding.ASCII.GetString(BitConverter.GetBytes(ChunkID)),
                ChunkSize,
                Encoding.ASCII.GetString(BitConverter.GetBytes(Format)),
                Encoding.ASCII.GetString(BitConverter.GetBytes(Subchunk1ID)),
                Subchunk1Size,
                (AudioFormat == 1) ? "1 : PCM" : ((AudioFormat == (UInt16)0xFFFE) ? "0xFFFE : WAVEFORMATEXTENSIBLE" : AudioFormat.ToString()),
                NumChannels,
                SampleRate,
                ByteRate,
                BlockAlign,
                BitsPerSample,
                ExtraParamSize,
                Samples,
                ChannelMask,
                GuidSubFormat.ToString() + " : " + ((GuidSubFormat == subTypePCM) ? "PCM" : ((GuidSubFormat == subTypeIEEE_FLOAT) ? "IEEE FLOAT" : "Unknown")),
                Encoding.ASCII.GetString(BitConverter.GetBytes(Subchunk2ID)),
                Subchunk2Size,
                IsExtensible,
                HeaderSize,
                TimeSpan.FromSeconds(Duration),
                TotalSamples);
        }
    }

    #endregion
}