using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeiligaoProtocol
{
    public class MeitrackPacket
    {
        public string Header { get; set; }

        public string PacketFlag { get; set; }

        public ushort Length { get; set; }

        public string IMEI { get; set; }

        public MeitrackCommand Command{ get; set;}

        public MeitrackData Data { get; set; }

        public ushort CheckSum { get; set; }








    }
}
