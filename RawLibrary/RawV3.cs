using System;
using System.Text;
using System.IO;

namespace RawLibrary
{
    public enum CameraSeries : ulong
    {
        XSeries = 0,
        SSeries = 1,
        TriSeries = 2,
        QSeries = 3,
        PromonSeries = 4
    }

    public enum SensorType : byte
    {
        // Bayer
        Mono = 0,
        BG_GR = 1,
        RG_GB = 2,
        GB_RG = 3,
        GR_BG = 4
    }

    public class RawV3
    {
        public struct RAWHEADERV3
        {                 // [pos] [size]
            public byte[] FileSignature;            //	0	    8	0x52 0x41 0x57 0x56 0x03 0x00 (hex values for null terminated ASCII string RAWV3 in network byte order)        
            public ulong FileVersion;               //	8	    8	1 = for v1.0 
            //       2 = for v2.0 
            //       11 = for v1.1 
            //       21 = for 2.1 
            //       111 = for v1.1.1 
            //       211 = 02.01.2001   
            public char[] FileCreator;              //	16	    64	Name of the application, which created this file 
            public CameraSeries CameraSeries;       //  80      8   Type of the Camera Series             
            public char[] CameraVendorName;         //	88	    56	Name of the camera vendor                     
            public char[] CameraModelName;          //	144	    64	Name of the camera model                     
            public char[] CameraID;                 //	208	    64	Serial number of the camera used to create this recording                
            public char[] CameraVersion;            //	272	    64	Version of the camera                      
            public char[] CameraFirmwareVersion;    //	336	    64	Camera firmware version                       
            public char[] CameraFPGAVersion;        //	400	    64	Camera FPGA version                       
            public char[] UserComment;              //	464	    512	User comment                        
            public char[] RecordingDate;            //	976	    16	Date of test (YYYY-MM-DD)                      
            public char[] RecordingTime;            //	992	    16	Time of test (hh:mm:ss)                      
            public ulong ImageDataHeaderSize;       //	1008	8	Byte offset to the FrameHeader Table                    
            public ulong FPNHeaderSize;             //	1016	8	Byte offset to the FPNHeader Table                    
            public ulong ImageDataSize_NotUsed;     //	1024	8	Byte offset to the frame data                    
            public SensorType ColorFilterType;      //	1032	1	Bayer
            public byte FrameType;                  //	1033	1	0 = Raw                       
            public byte OSD;                        //	1034	1	OSD activated / deactivated                      
            public byte FPN;                        //	1035	1	FPN activated / deactivated                      
            public uint Pixelsize;                  //	1036	4	Size of pixel                       
            public ulong FrameCount;                //	1040	8	Count of frames stored in this file                   
            public ulong ImageSize;                 //	1048	8	Size of the Image Data including Image Data Header    
            public uint FrameWidth;                 //	1056	4	Frame width in pixelsFrame 
            public uint FrameHeight;                //  1060    4   Frame height in pixels      
            public uint SensorFrameWidth;           //	1064	4	Width of the frame on the sensor in pixel                 
            public uint SensorFrameHeight;          //	1068	4	Height of the frame on the sensor in pixel                 
            public uint SyncSource;                 //	1072	4	0: internal sync; 1: external sync 2: IRIG sync                 
            public uint SyncRate;                   //	1076	4	actual recording rate fps                      
            public uint ShutterValue;               //	1080	4	shutter time in microseconds, 0 if unknown                   
            public uint TimeSource;                 //	1084	4	0: None, 1:0 Host Clock, 2: Camera Clock, 3: IRIG                
            public uint Frame0TimeStamp;            //	1088	4	Timestamp of Frame 0 in seconds in the original recording                
            public uint Frame0SubSec;               //	1092	4	Sub seconds of Frame 0 in units of 100ns in the original recording             
            public uint T0TimeStamp;                //	1096	4	Timestamp of trigger signal detection in seconds in the original recording               
            public uint T0SubSec;                   //	1100	4	Sub seconds of trigger signal detection in units of 100ns in the original recording            
            public ulong T0FrameIndex;              //	1104	8	Index of the T0 frame in this file, if contained. -1 otherwise              
            public ulong T0Frame;                   //	1112	8	Index of T0 frame in the original recording                  
            public ulong OriginalSequenceLength;    //	1120	8	Sequence length in the original recording                    
            public ulong MarkIn;                    //	1128	8	Mark IN of the original recording                    
            public ulong MarkOut;                   //	1136	8	Mark OUT of the original recording                    
            public uint BitCount;                   //	1144	4	Bits per pixel                       
            public ushort BitMode;                  //	1148	2	low / mid / high                     
            public ushort Autoexposure;             //	1150	2	Autoexposure activated / deactivated                      
            public ushort EnableHorizontalFlip;     //	1152	2	Horizontal Flip activated / deactivated                     
            public ushort EnableVerticalFlip;       //	1154	2	Vertical Flip activated / deactivated                     
            public uint ImageRotation;              //	1156	4	The Image Rotation Value                      
            public ushort EnableEventmarkers;       //	1160	2	Eventmarkers activated / deactivated                      
            public ushort PositionEventmarkers;     //	1162	2	Onscreen Position Eventmarkers 0: TopLeft, 1: TopRight, 2: BottomRight, 3: BottomLeft               
            public uint NumberofEventmarkers;       //	1164	4	Number of available Eventmarkers                      
            public ushort OsdMode;                  //	1168	2	OSD Mode 
            //          000001: Enable Logo  
            //          000010: Enable Comment  
            //          000100: Enable Cameraname  
            //          001000: Enable Resoltion / FPS  
            //          010000: Enable Framenumber / Frame Time  
            //          100000: Enable IRIG-B Time
            public ushort OsdPosition;              //	1170	2	On screen Position Eventmarkers 0: TopLeft, 1: TopRight, 2: BottomRight, 3: BottomLeft              
            public uint OsdFontSize;                //	1172	4	Font Size of the OSD                     
            public char[] OsdFont;                  //	1176	40	Font Type of the OSD                     
            public char[] OsdFontStyle;             //	1216	64	Font Style of the OSD                     
            public char[] OsdColor;                 //	1280	40	Font Color of the OSD                     
            public byte ImageCorrection;            //  1320    1
            public byte RedChannel;                 //  1321    1
            public byte GreenChannel;               //  1322    1
            public byte BlueChannel;                //  1323    1
            public byte Brightness;                 //  1324    1
            public byte Contrast;                   //  1325    1
            public byte Gamma;                      //  1326    1
            public byte Saturation;                 //  1327    1
            public byte Hdr;                        //  1328    1
            public byte[] Free;                     //	1329	2767	FREE

            public const int Size = 4096;
        }

        private readonly BinaryReader _binaryReader;
        private RAWHEADERV3 _rawHeader;
        private byte[] _fpnOffsetAndGain;

        /// <summary>
        /// Number of the next frame that will be read, not the one that was last read.
        /// </summary>
        public ulong CurrentFrame
        {
            get;
            private set;
        }
        public ulong NofFrames => _rawHeader.FrameCount;
        public uint Width => _rawHeader.SensorFrameWidth;
        public uint Height => _rawHeader.SensorFrameHeight;
        public uint ImageWidth => _rawHeader.FrameWidth;
        public uint ImageHeight => _rawHeader.FrameHeight;
        public ulong FrameSizeInBytes => _rawHeader.ImageSize;
        public SensorType SensorType => (SensorType)Enum.ToObject(typeof(SensorType), (int)_rawHeader.ColorFilterType);
        public byte[] Data { get; private set; }
        public uint BitsPerPixel => _rawHeader.BitCount;
        public ushort BitMode => _rawHeader.BitMode;
        public byte[] FrameHeader { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename">rawfilename</param>
        public RawV3(string filename)
        {
            _binaryReader = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read), Encoding.ASCII);
            ReadHeader();
            CurrentFrame = 0;
        }

        public void Close()
        {
            _binaryReader.Close();
        }

        /// <summary>
        /// Open a raw file and read the headerinformation (first 1024 bytes)
        /// </summary>
        private void ReadHeader()
        {
            _rawHeader = new RAWHEADERV3
            {
                FileSignature = _binaryReader.ReadBytes(8),
                FileVersion = _binaryReader.ReadUInt64(),
                FileCreator = _binaryReader.ReadChars(64),
                CameraSeries = (CameraSeries)_binaryReader.ReadUInt64(),
                CameraVendorName = _binaryReader.ReadChars(56),
                CameraModelName = _binaryReader.ReadChars(64),
                CameraID = _binaryReader.ReadChars(64),
                CameraVersion = _binaryReader.ReadChars(64),
                CameraFirmwareVersion = _binaryReader.ReadChars(64),
                CameraFPGAVersion = _binaryReader.ReadChars(64),
                UserComment = _binaryReader.ReadChars(512),
                RecordingDate = _binaryReader.ReadChars(16),
                RecordingTime = _binaryReader.ReadChars(16),
                ImageDataHeaderSize = _binaryReader.ReadUInt64(),
                FPNHeaderSize = _binaryReader.ReadUInt64(),
                ImageDataSize_NotUsed = _binaryReader.ReadUInt64(),
                ColorFilterType = (SensorType)_binaryReader.ReadByte(),
                FrameType = _binaryReader.ReadByte(),
                OSD = _binaryReader.ReadByte(),
                FPN = _binaryReader.ReadByte(),
                Pixelsize = _binaryReader.ReadUInt32(),
                FrameCount = _binaryReader.ReadUInt64(),
                ImageSize = _binaryReader.ReadUInt64(),
                FrameWidth = _binaryReader.ReadUInt32(),
                FrameHeight = _binaryReader.ReadUInt32(),
                SensorFrameWidth = _binaryReader.ReadUInt32(),
                SensorFrameHeight = _binaryReader.ReadUInt32(),
                SyncSource = _binaryReader.ReadUInt32(),
                SyncRate = _binaryReader.ReadUInt32(),
                ShutterValue = _binaryReader.ReadUInt32(),
                TimeSource = _binaryReader.ReadUInt32(),
                Frame0TimeStamp = _binaryReader.ReadUInt32(),
                Frame0SubSec = _binaryReader.ReadUInt32(),
                T0TimeStamp = _binaryReader.ReadUInt32(),
                T0SubSec = _binaryReader.ReadUInt32(),
                T0FrameIndex = _binaryReader.ReadUInt64(),
                T0Frame = _binaryReader.ReadUInt64(),
                OriginalSequenceLength = _binaryReader.ReadUInt64(),
                MarkIn = _binaryReader.ReadUInt64(),
                MarkOut = _binaryReader.ReadUInt64(),
                BitCount = _binaryReader.ReadUInt32(),
                BitMode = _binaryReader.ReadUInt16(),
                Autoexposure = _binaryReader.ReadUInt16(),
                EnableHorizontalFlip = _binaryReader.ReadUInt16(),
                EnableVerticalFlip = _binaryReader.ReadUInt16(),
                ImageRotation = _binaryReader.ReadUInt32(),
                EnableEventmarkers = _binaryReader.ReadUInt16(),
                PositionEventmarkers = _binaryReader.ReadUInt16(),
                NumberofEventmarkers = _binaryReader.ReadUInt32(),
                OsdMode = _binaryReader.ReadUInt16(),
                OsdPosition = _binaryReader.ReadUInt16(),
                OsdFontSize = _binaryReader.ReadUInt32(),
                OsdFont = _binaryReader.ReadChars(40),
                OsdFontStyle = _binaryReader.ReadChars(64),
                OsdColor = _binaryReader.ReadChars(40),
                ImageCorrection = _binaryReader.ReadByte(),
                RedChannel = _binaryReader.ReadByte(),
                GreenChannel = _binaryReader.ReadByte(),
                BlueChannel = _binaryReader.ReadByte(),
                Brightness = _binaryReader.ReadByte(),
                Contrast = _binaryReader.ReadByte(),
                Gamma = _binaryReader.ReadByte(),
                Saturation = _binaryReader.ReadByte(),
                Hdr = _binaryReader.ReadByte(),
                Free = _binaryReader.ReadBytes(2767)
            };

            // correct SensorFrameSize
            _rawHeader.ImageSize = ((_rawHeader.SensorFrameWidth * _rawHeader.SensorFrameHeight) * _rawHeader.BitCount / 8);
            Data = new byte[_rawHeader.ImageSize];

            // read gain offset and coefficients
            if (_rawHeader.FPN != 0)
            {
                _fpnOffsetAndGain = _binaryReader.ReadBytes((int)_rawHeader.FPNHeaderSize);
            }
        }

        private uint LRot(uint v, int n)
        {
            return (v << n) | (v >> (32 - n));
        }

        private uint ByteSwap(uint v)
        {
            return LRot(((v & 0xFF00FF00) >> 8) | ((v & 0x00FF00FF) << 8), 16);
        }

        private short ByteSwap(short v)
        {
            return (short)(((v >> 8) & 0xFF) | (v << 8));
        }

        private byte Clamp8(int v)
        {
            // needs only one test in the normal case
            if ((v & 0xFF00) != 0) return (v < 0) ? (byte)0 : (byte)255;
            return (byte)v;
        }

        /// <summary>
        /// Reads the next frame of a video.
        /// </summary>
        public void ReadFrame()
        {
            if (CurrentFrame >= NofFrames) return;

            // read image data header
            FrameHeader = _binaryReader.ReadBytes((int)_rawHeader.ImageDataHeaderSize);

            // read image data
            Data = _binaryReader.ReadBytes((int)FrameSizeInBytes);

            // Adapt byte order
            if (_rawHeader.CameraSeries != CameraSeries.QSeries && _rawHeader.CameraSeries != CameraSeries.PromonSeries)
            {
                int maxLength = (int)FrameSizeInBytes;
                for (int i = 0; i < maxLength; i += 4)
                {
                    // pixel rotation inside a 32Bit image data word
                    byte tmp = Data[i]; Data[i] = Data[i + 3]; Data[i + 3] = tmp;
                    tmp = Data[i + 1]; Data[i + 1] = Data[i + 2]; Data[i + 2] = tmp;
                }
            }

            // Remove fixed pattern noise if necessary
            if (_rawHeader.FPN == 1 && _fpnOffsetAndGain != null) FpnRemoval();

            CurrentFrame++;
        }

        /// <summary>
        /// Reads a specific frame of a video, using Seek.
        /// </summary>
        /// <param name="framenumber"> Number of the frame to be read. </param>
        public void ReadFrame(ulong framenumber)
        {
            if (framenumber >= NofFrames) return;
            CurrentFrame = framenumber;

            // adjust pointer accordingly to read the desired frame
            long seekOffset = RAWHEADERV3.Size + (long)_rawHeader.FPNHeaderSize + (long)CurrentFrame * (long)(_rawHeader.ImageDataHeaderSize + FrameSizeInBytes);
            _binaryReader.BaseStream.Seek(seekOffset, SeekOrigin.Begin);

            ReadFrame();
        }

        public bool ReadPreviousFrame()
        {
            if (CurrentFrame <= 1) return false;
            CurrentFrame -= 2;
            ReadFrame(CurrentFrame);
            return true;
        }

        /// <summary>
        /// Removes Fixed Pattern Noise
        /// </summary>
        private void FpnRemoval()
        {
            if (_rawHeader.CameraSeries == CameraSeries.SSeries || _rawHeader.FPNHeaderSize == 5308416)
            {
                //// S-Series
                const int sensorWidth = 1296; //horizontal sensor resolution
                const int sensorHeight = 1024;//vertical sensor resolution
                const int kernelSize = 24; //sensors kernel size
                const int coeffFactor = 1600; //constant fpn - coefficient factor 

                uint sensorFrameWidth = _rawHeader.SensorFrameWidth; //rounded up image width
                uint sensorFrameHeight = _rawHeader.SensorFrameHeight; //the image height
                ushort bitMode = _rawHeader.BitMode; //bitMode 0 means lowGainMode, 1=middle, 2=high
                uint coeffOffSetX = (uint)(sensorWidth / 2 - kernelSize * Math.Ceiling(sensorFrameWidth / kernelSize / 2.0f));
                uint coeffOffsetY = (uint)(2 * Math.Floor((sensorHeight - sensorFrameHeight + 1) / 4.0f));
                uint lineStartOut = 0;
                uint lineStartCoeff = coeffOffSetX + coeffOffsetY * sensorWidth + 1;

                for (int y = 0; y < sensorFrameHeight; y++)
                {
                    uint pos = lineStartOut;
                    uint posCoeff = lineStartCoeff;

                    for (int x = 0; x < sensorFrameWidth; x++, pos++, posCoeff++)
                    {
                        uint fpnIndex = posCoeff * 2;
                        short c = BitConverter.ToInt16(_fpnOffsetAndGain, (int)fpnIndex * 2);
                        short b = BitConverter.ToInt16(_fpnOffsetAndGain, (int)(fpnIndex + 1) * 2);

                        //if input value is saturated, no fpn correction is calculated (set output to fix value) 
                        if (0 < Data[pos] && Data[pos] < 255)
                        {
                            Data[pos] = Clamp8(((c << bitMode) + b * Data[pos]) / coeffFactor);
                        }
                    }
                    lineStartCoeff += sensorWidth;
                    lineStartOut += sensorFrameWidth;
                }

            }
            else if (_rawHeader.CameraSeries == CameraSeries.QSeries || _rawHeader.FPNHeaderSize == 17400960)
            {
                //// Q-Series
                const int sensorWidth = 1696; //horizontal sensor resolution
                const int sensorHeight = 1710; //vertical sensor resolution
                const int kernelSize = 32; //sensors kernel size
                const int cF = 8, cE = 4;

                uint sensorFrameWidth = _rawHeader.SensorFrameWidth; //rounded up image width
                uint sensorFrameHeight = _rawHeader.SensorFrameHeight; //the image height
                uint coeffOffSetX = (uint)(sensorWidth / 2 + 1.5 * kernelSize - Math.Ceiling((sensorFrameWidth + 3 * kernelSize) / (4.0f * kernelSize)) * 2 * kernelSize);
                uint coeffOffsetY = (uint)(2 * Math.Floor((sensorHeight - sensorFrameHeight + 1) / 4.0f));
                uint lineStartOut = 0;
                uint lineStartCoeff = coeffOffSetX + coeffOffsetY * sensorWidth;

                for (int y = 0; y < sensorFrameHeight; y++)
                {
                    uint pos = lineStartOut;
                    uint posCoeff = lineStartCoeff;

                    for (int x = 0; x < sensorFrameWidth; x++, pos++, posCoeff++)
                    {
                        //if input value is saturated, no fpn correction is calculated (set output to fix value) 
                        if (0 < Data[pos] && Data[pos] < 255)
                        {
                            uint fpnIndex = posCoeff * 3;
                            short c = BitConverter.ToInt16(_fpnOffsetAndGain, (int)fpnIndex * 2);
                            short b = BitConverter.ToInt16(_fpnOffsetAndGain, (int)(fpnIndex + 1) * 2);
                            short a = BitConverter.ToInt16(_fpnOffsetAndGain, (int)(fpnIndex + 2) * 2);

                            Data[pos] = Clamp8(((((((a * Data[pos]) >> cF) + b) * Data[pos]) >> cE) + c) >> cF);
                        }
                    }
                    lineStartOut += sensorFrameWidth;
                    lineStartCoeff += sensorWidth;
                }
            }
        }
    }
}
