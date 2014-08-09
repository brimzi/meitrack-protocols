using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using MeiligaoProtocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MeiligaoProtocolTest
{
    [TestClass]
    public class ProcessorUnitTest
    {
        /// <summary>
        /// Tests the who packet construction using raw bytes.This checks to comfirm that a packet can be created by using pre determined raw bytes,it does not cover all corner cases
        /// therefore unit tests should be used to catch those
        /// </summary>
        [TestMethod]
        public void TestGetMeiligaoPacket()
        {
            var trackOnDemandresponsePacket = new MeiligaoPacket();
            trackOnDemandresponsePacket.Header = "$$";
            trackOnDemandresponsePacket.ID = "123456";
            trackOnDemandresponsePacket.Length = 96;
            trackOnDemandresponsePacket.GpsData = new GPSData() { Altitude = 0.0, HDOP = "11.5", GPRMC = new GPRMC("035644.000,A,2232.6083,N,11404.8137,E,0.00,,010809,,*1C") };
            trackOnDemandresponsePacket.Command = CommandTypes.TrackOnDemandResponse;
            var trackOnDemandresponseBytes = new byte[]
                                        {
                                            0x24, 0x24, 0x00, 0x60, 0x12, 0x34, 0x56, 0xFF, 0xFF, 0xFF, 0xFF, 0x99, 0x55
                                            , 0x30, 0x33, 0x35, 0x36, 0x34, 0x34, 0x2E, 0x30
                                            , 0x30, 0x30, 0x2C, 0x41, 0x2C, 0x32, 0x32, 0x33, 0x32, 0x2E, 0x36, 0x30,
                                            0x38, 0x33, 0x2C, 0x4E, 0x2C, 0x31, 0x31, 0x34, 0x30, 0x34
                                            , 0x2E, 0x38, 0x31, 0x33, 0x37, 0x2C, 0x45, 0x2C, 0x30, 0x2E, 0x30, 0x30,
                                            0x2C, 0x2C, 0x30, 0x31, 0x30, 0x38, 0x30, 0x39, 0x2C, 0x2C, 0x2A, 0x31
                                            , 0x43, 0x7C, 0x31, 0x31, 0x2E, 0x35, 0x7C, 0x31, 0x39, 0x34, 0x7C, 0x30,
                                            0x30, 0x30, 0x30, 0x7C, 0x30, 0x30, 0x30, 0x30, 0x2C, 0x30, 0x30, 0x30
                                            , 0x30, 0x69, 0x62, 0x0D, 0x0A
                                        };

            var testPacket1 = MeiligaoProcessor.GetMeiligaoPacket(trackOnDemandresponseBytes);

            Assert.AreEqual(testPacket1.Header, trackOnDemandresponsePacket.Header,"Header convension test failed");
            Assert.AreEqual(testPacket1.Length, trackOnDemandresponsePacket.Length,"Length convension test failed");
            Assert.AreEqual(testPacket1.Command, trackOnDemandresponsePacket.Command,"Command convension test failed");
            Assert.AreEqual(testPacket1.GpsData.Altitude, trackOnDemandresponsePacket.GpsData.Altitude, "Command convension test failed");
            Assert.AreEqual(testPacket1.GpsData.HDOP, trackOnDemandresponsePacket.GpsData.HDOP, "HDOP convension test failed");
            Assert.AreEqual(testPacket1.GpsData.GPRMC.GPRMCSentenceString, trackOnDemandresponsePacket.GpsData.GPRMC.GPRMCSentenceString, "GPRMC convension test failed");

            Assert.AreEqual(testPacket1.GpsData.GPRMC.CheckSum, trackOnDemandresponsePacket.GpsData.GPRMC.CheckSum, "GPRMC-CheckSum convension test failed");
            Assert.AreEqual(testPacket1.GpsData.GPRMC.D, trackOnDemandresponsePacket.GpsData.GPRMC.D, "GPRMC-D convension test failed");
            Assert.AreEqual(testPacket1.GpsData.GPRMC.DateTimeUTC, trackOnDemandresponsePacket.GpsData.GPRMC.DateTimeUTC, "GPRMC-DateTimeUTC convension test failed");
            Assert.AreEqual(testPacket1.GpsData.GPRMC.Direction, trackOnDemandresponsePacket.GpsData.GPRMC.Direction, "GPRMC-Direction convension test failed");
            Assert.AreEqual(testPacket1.GpsData.GPRMC.Latitude, trackOnDemandresponsePacket.GpsData.GPRMC.Latitude, "GPRMC-Latitude convension test failed");
            Assert.AreEqual(testPacket1.GpsData.GPRMC.Longitude, trackOnDemandresponsePacket.GpsData.GPRMC.Longitude, "GPRMC-Longitude convension test failed");
            Assert.AreEqual(testPacket1.GpsData.GPRMC.Speed, trackOnDemandresponsePacket.GpsData.GPRMC.Speed, "GPRMC-Speed convension test failed");
            Assert.AreEqual(testPacket1.GpsData.GPRMC.Status, trackOnDemandresponsePacket.GpsData.GPRMC.Status, "GPRMC-Status convension test failed");
        }

        [TestMethod]
        public void TestChecksum()
        {
            var bytes1 = new byte[] {0x24, 0x24, 0x00, 0x11, 0x13, 0x61, 0x23, 0x45, 0x67, 0x8f, 0xff, 0x50, 0x00};
            var expectedChecksum1 = new byte[] {0x05, 0xd8};
            var checksum1=ChecksumUtility.ComputeMeiligaoChecksum(bytes1);
            Assert.AreEqual(BitConverter.ToString(expectedChecksum1),BitConverter.ToString(checksum1));


            var bytes2=new byte[]{0x24, 0x24, 0x00, 0x11, 0x13, 0x61, 0x23, 0x45, 0x67, 0x8f, 0xff, 0x50, 0x00, 0x05 ,0xd8 ,0x0d ,0x0a};
            var checksum2 = ChecksumUtility.ComputeMeiligaoChecksum(bytes2, 0, 13);
            Assert.AreEqual(BitConverter.ToString(expectedChecksum1), BitConverter.ToString(checksum2));
        }
    }
}
