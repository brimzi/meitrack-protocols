using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MeiligaoProtocol
{
    public class MeiligaoProcessor
    {

        public static byte[] GetBytes(MeiligaoPacket Packet)
        {
            var bytesList = new List<byte>();
            //1.header
            var headerBytes = Encoding.ASCII.GetBytes(Packet.Header);
            bytesList.AddRange(headerBytes);

            //2.Length
            var intBytes = BitConverter.GetBytes(Packet.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            bytesList.AddRange(intBytes);

            //3.ID
            var id = getIDBytes(Packet.ID);
            bytesList.AddRange(id);

            //4.Command
            var commandByte = BitConverter.GetBytes((ushort) Packet.Command);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(commandByte);
            bytesList.AddRange(commandByte);

            //5.Parameter/Data?


            //6.Checksum
            var checksumBytes = getCheckSumforBytes(bytesList);
            bytesList.AddRange(checksumBytes);


            //Ending
            bytesList.AddRange(new byte[] {0x0d, 0x0a});

            return bytesList.ToArray();
        }

        public static MeiligaoPacket GetMeiligaoPacket(byte[] bytes)
        {
            UInt16 validChecksum;
            if (!IsChecksumValid(bytes, out validChecksum)) return null;

            var packet = new MeiligaoPacket();
            //1.Header
            packet.Header = Encoding.ASCII.GetString(bytes, 0, 2);

            //2.Length of the packet
            packet.Length = getLengthFromBytes(bytes);

            //3.ID
            packet.ID = getIDFromBytes(bytes);

            //4.Command
            packet.Command = getCommandFromPacketBytes(bytes);


            var dataCount = bytes.Length - 13 - 2 - 2;
            if (dataCount == 1)
            {
                //Flag
                packet.Flag = getCommandFlagFromBytes(bytes);
            }
            else
            {
                //Data
                packet.GpsData = getGPSData(bytes);
            }


            packet.CheckSum = validChecksum;
            return packet;
        }

        private static byte getCommandFlagFromBytes(byte[] bytes)
        {
            return bytes[13];//Flags are only one byte long and we know where they are always located
        }

        private static bool IsChecksumValid(byte[] bytes, out UInt16 ValidChecksum)
        {
            ValidChecksum = 0;

            var calculatedChecksum = getCheckSumforBytes(bytes, 0, bytes.Length - 4);
            var checksumInPacket = new[] {bytes[bytes.Length - 4], bytes[bytes.Length - 3]};
            var c = BitConverter.ToUInt16(calculatedChecksum, 0);
            var d = BitConverter.ToUInt16(checksumInPacket, 0);
            if (c != d) return false;

            ValidChecksum = c;
            return true;
        }

        private static ushort getLengthFromBytes(byte[] packetBytes)
        {
            var firstByte = 2;
            var secondByte = 3;
            if (BitConverter.IsLittleEndian)
            {
                firstByte = 3;
                secondByte = 2;
            }
            var commandBytes = new[] {packetBytes[firstByte], packetBytes[secondByte]};
            return BitConverter.ToUInt16(commandBytes, 0);
        }

        private static GPSData getGPSData(byte[] bytes)
        {
            var dataCount = bytes.Length - 13 - 2 - 2;
            if (dataCount < 1) return null;
            var dataSection = Encoding.ASCII.GetString(bytes, 13, dataCount);

            var dataComponents = dataSection.Split('|');

            if (dataComponents.Length == 0) return null;

            var gpsData = new GPSData();
            gpsData.GPRMC = new GPRMC(dataComponents[0]);

            //if (!gpsData.GPRMC.ChecksumMatched) return null; //we are very strict on integrity here

            gpsData.HDOP = dataComponents[1];
            var altitude = 0.0;
            if (double.TryParse(dataComponents[3], out altitude))
                gpsData.Altitude = altitude;


            return gpsData;
        }

        private static CommandTypes getCommandFromPacketBytes(byte[] packetBytes)
        {
            var rtVal = CommandTypes.None;
            var firstByte = 11;
            var secondByte = 12;
            if (BitConverter.IsLittleEndian)
            {
                firstByte = 12;
                secondByte = 11;
            }
            var commandBytes = new Byte[] {packetBytes[firstByte], packetBytes[secondByte]};
            var b = BitConverter.ToUInt16(commandBytes, 0).ToString();
            CommandTypes.TryParse(b, out rtVal);
            return rtVal;
        }

        private static string getIDFromBytes(byte[] bytes)
        {
            var rtVal = string.Empty;
            rtVal = BitConverter.ToString(bytes, 4, 7).Replace("-", "");
            rtVal = rtVal.ToLower().Remove(rtVal.ToLower().IndexOf('f'));
            return rtVal;
        }

        private static byte[] getCheckSumforBytes(IEnumerable<byte> bytes)
        {
            return ChecksumUtility.ComputeMeiligaoChecksum(bytes.ToArray());
        }

        private static byte[] getCheckSumforBytes(IEnumerable<byte> bytes, int StartIndex, int Count)
        {
            return ChecksumUtility.ComputeMeiligaoChecksum(bytes.ToArray(), StartIndex, Count);
        }

        private static IEnumerable<byte> getIDBytes(string Id)
        {
            var bytesList = new List<byte>();
            for (var i = 0; i < Id.Length; i += 2)
            {
                var s = string.Empty;
                if ((Id.Length - i) == 1)
                {
                    s = Id.Substring(i, 1);
                    s = s + "f";
                }
                else
                    s = Id.Substring(i, 2);

                byte value;
                if (byte.TryParse(s, NumberStyles.HexNumber, null, out value))
                {
                    bytesList.Add(value);
                }
            }
            if (bytesList.Count < 7)
            {
                while (bytesList.Count < 7)
                {
                    var s = "ff";
                    bytesList.Add(byte.Parse(s, NumberStyles.HexNumber, null));
                }
            }
            return bytesList;
        }
    }

    public class MeiligaoPacket
    {
        public string Header { get; set; }

        public ushort Length { get; set; }

        public string ID { get; set; }

        public CommandTypes Command { get; set; }

        public GPSData GpsData { get; set; }

        public ushort CheckSum { get; set; }

        public byte[] DataBytes { get; set; }

        public byte Flag { get; set; }
    }

    public class GPSData
    {
        public GPRMC GPRMC { get; set; }

        public string HDOP { get; set; }

        public double Altitude { get; set; }
    }

    public class GPRMC
    {
        public enum SatelliteFix : short
        {
            Invalid = 0,
            Valid = 1
        }

        private bool _checksumMatched = false;

        public GPRMC(string GPRMCSentence)
        {
            _GPRMCSentenceString = GPRMCSentence;
            populate(GPRMCSentence);
        }

        #region Properties
        private string _GPRMCSentenceString;
        public string GPRMCSentenceString
        {
            get { return _GPRMCSentenceString; }
        }

        public bool ChecksumMatched
        {
            get { return _checksumMatched; }
        }

        public DateTime DateTimeUTC { get; set; }

        //public DateTime DateTimeLocal { get; set; }

        public SatelliteFix Status { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double _speed;
        public double Speed { get { return _speed; } set { _speed = value; } }

        private double _direction;
       
        public double Direction { get { return _direction; } set { _direction = value; } }

        //public DateTime Date { get; set; }

        public string MagneticVariation { get; set; }

        public string D { get; set; }

        public int CheckSum { get; set; }

        #endregion

        #region Private Methods

        private void populate(string gprmcSentence)
        {
            var stringParts = gprmcSentence.Split(',');
            setDateTime(stringParts[8], stringParts[0]);
            setGPSStatus(stringParts[1]);
            setLatitude(stringParts[2], stringParts[3]);
            setLongitude(stringParts[4], stringParts[5]);
            setSpeed(stringParts[6]);
            setDirection(stringParts[7]);
            setMagneticVariation(stringParts[9]);
            var c=stringParts[10].Split('*');
            setD(c[0]);
            checkAndSetChecksum(c[1],gprmcSentence);
        }

        private void checkAndSetChecksum(string checksumFromGPRMCString,string gprmcSentence)
        {
            int checksumInt = 0;
            if(!gprmcSentence.StartsWith("GPRMC")) gprmcSentence = "GPRMC," + gprmcSentence;
            foreach (var character in gprmcSentence)
            {
                if (character == '*') break;

                if (checksumInt == 0)
                    checksumInt = Convert.ToByte(character);
                else
                    checksumInt = checksumInt ^ Convert.ToByte(character);
            }
            var calculatedChecksum = checksumInt.ToString("X2");
        

            if (calculatedChecksum.ToLower() == checksumFromGPRMCString.ToLower())
            {
                CheckSum = checksumInt;
                _checksumMatched = true;
            }
        }

        private void setD(string stringPart)
        {
            D = stringPart;
        }

        private void setMagneticVariation(string stringPart)
        {
            MagneticVariation = stringPart;
        }

        private void setDirection(string directionString)
        {
            double.TryParse(directionString, out _direction);
        }

        private void setSpeed(string speedString)
        {
          double.TryParse(speedString, out _speed);
        }

        private void setLongitude(string stringPart, string sideofMeridian)
        {
            var sideIndicator = 1;
            if (sideofMeridian.ToLower() == "w")
                sideIndicator = -1;

            var deg = double.Parse(stringPart.Substring(0, 3));
            var min = double.Parse(stringPart.Substring(3));
            Longitude = (deg + (min/60))*sideIndicator;
        }

        private void setLatitude(string stringPart, string hemisphere)
        {
            var hemIndicator = 1;
            if (hemisphere.ToLower() == "s")
                hemIndicator = -1;

            var deg = double.Parse(stringPart.Substring(0, 2));
            var min = double.Parse(stringPart.Substring(2));
            Latitude = (deg + (min/60))*hemIndicator;
        }

        private void setGPSStatus(string statusString)
        {
            if (statusString.ToLower() == "a")
                Status = SatelliteFix.Valid;
            else if (statusString.ToLower() == "v")
                Status = SatelliteFix.Invalid;
        }

        private void setDateTime(string DatePart, string TimePart)
        {
            //hhmmss
            var hour = int.Parse(TimePart.Substring(0, 2));
            var min = int.Parse(TimePart.Substring(2, 2));
            var secParts = TimePart.Substring(4, 4).Split('.');
            var sec = int.Parse(secParts[0]);
            var secDecimal = int.Parse(secParts[1])*10;

            //ddmmyy
            var day = int.Parse(DatePart.Substring(0, 2));
            var month = int.Parse(DatePart.Substring(2, 2));
            var year = int.Parse("20"+DatePart.Substring(4, 2));

            DateTimeUTC = new DateTime(year, month, day, hour, min, sec, secDecimal);
        }

        #endregion
    }

    public enum CommandTypes : ushort
    {
        None = 0,
        Login = 0x5000,
        LoginConfirmation = 0x4000,
        TrackOnDemand = 0x4101,
        TrackByInterval = 0x4102,
        Autorization = 0x4103,
        SpeedAlarm = 0x4105,
        MovementAlarm = 0x4106,
        ExtendedSettings = 0x4108,
        Initialization = 0x4110,
        SleepMode = 0x4113,
        OutputControlConditional1 = 0x4114,
        OutputControlConditional2 = 0x5114,
        OutputControlImmediate = 0x4115,
        TriggeredAlarm = 0x4116,
        PowerDown = 0x4126,
        ListenInVoiceMonitoring = 0x4130,
        LogByInterval = 0x4130,
        TimeZone = 0x4132,
        SetTrembleSensorSensitivity = 0x4135,
        HeadingChangeReport = 0x4136,
        SetGPSAntennaCutAlarm = 0x4150,
        SetGPRSParameters = 0x4155,
        SetGeoFenceAlarm = 0x4302,
        TrackByDistance = 0x4303,
        DeleteMileage = 0x4351,
        RebootGPS = 0x4902,
        HeartBeat = 0x5199,
        ClearMessageQueue = 0x5503,
        GetSNIMEI = 0x9001,
        ReadInterval = 0x9002,
        ReadAuthorization = 0x9003,
        ReadLoggedData = 0x9016,
        TrackOnDemandResponse = 0x9955,
        Alarms = 0x9999
    }

    public enum TrackerAlarmTypes
    {
    }
}