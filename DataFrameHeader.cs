using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocketServer
{
    public class DataFrameHeader
    {
        private bool _fin;
        private bool _rsv1;
        private bool _rsv2;
        private bool _rsv3;
        private sbyte _opcode;
        private bool _maskcode;
        private sbyte _payloadlength;

        public bool FIN { get { return _fin; } }

        public bool RSV1 { get { return _rsv1; } }

        public bool RSV2 { get { return _rsv2; } }

        public bool RSV3 { get { return _rsv3; } }

        public sbyte OpCode { get { return _opcode; } }

        public bool HasMask { get { return _maskcode; } }

        public sbyte Length { get { return _payloadlength; } }
   /* 0                   1                   2                   3
      0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
     +-+-+-+-+-------+-+-------------+-------------------------------+
     |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
     |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
     |N|V|V|V|       |S|             |   (if payload len==126/127)   |
     | |1|2|3|       |K|             |                               |
     +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
     |     Extended payload length continued, if payload len == 127  |
     + - - - - - - - - - - - - - - - +-------------------------------+
     |                               |Masking-key, if MASK set to 1  |
     +-------------------------------+-------------------------------+
     | Masking-key (continued)       |          Payload Data         |
     +-------------------------------- - - - - - - - - - - - - - - - +
     :                     Payload Data continued ...                :
     + - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
     |                     Payload Data continued ...                |
     +---------------------------------------------------------------+
            */

        public DataFrameHeader(byte[] buffer)
        {
            if(buffer.Length<2)
                throw new Exception("无效的数据头.");

            //第一个字节 与运算:当且仅当两个操作数均为 true 时，结果才为 true
            _fin = (buffer[0] & 0x80) == 0x80;          //‭0x80:10000000‬、128   是否是最后一帧
            _rsv1 = (buffer[0] & 0x40) == 0x40;         //0x40:‭01000000‬、64    1bit,保留位，必须为0，如果不为0，则标记为连接失败
            _rsv2 = (buffer[0] & 0x20) == 0x20;         //‭0x20:00100000‬、32    1bit,保留位，必须为0，如果不为0，则标记为连接失败
            _rsv3 = (buffer[0] & 0x10) == 0x10;         //0x10:00010000、16    1bit,保留位，必须为0，如果不为0，则标记为连接失败
            _opcode = (sbyte)(buffer[0] & 0x0f);        //‭0x0f:00001111、15    4bit,操作位，定义这一帧的类型

            //第二个字节
            _maskcode = (buffer[1] & 0x80) == 0x80;                          //1bit,承载的内容是否需要用掩码进行异或
            _payloadlength = (sbyte)(buffer[1] & 0x7f); //‭0x7f:01111111‬、127 //7bit or 7 +16bit or 7 + 64bit 承载内容的长度

        }

        //发送封装数据。前2字节
        /* 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
          +-+-+-+-+-------+-+-------------+
          |F|R|R|R| opcode|M| Payload len |
          |I|S|S|S|  (4)  |A|     (7)     |
          |N|V|V|V|       |S|             |
          | |1|2|3|       |K|             |
          +-+-+-+-+-------+-+-------------+
        */
        public DataFrameHeader(bool fin,bool rsv1,bool rsv2,bool rsv3,sbyte opcode,bool hasmask,int length)
        {
            _fin = fin;
            _rsv1 = rsv1;
            _rsv2 = rsv2;
            _rsv3 = rsv3;
            _opcode = opcode;
            //第二个字节
            _maskcode = hasmask;
            _payloadlength = (sbyte)length;
        }

        //返回帧头字节
        public byte[] GetBytes()
        {
            byte[] buffer = new byte[2]{0,0};

            if (_fin) buffer[0] ^= 0x80;
            if (_rsv1) buffer[0] ^= 0x40;
            if (_rsv2) buffer[0] ^= 0x20;
            if (_rsv3) buffer[0] ^= 0x10;

            buffer[0] ^= (byte)_opcode;

            if (_maskcode) buffer[1] ^= 0x80;

            buffer[1] ^= (byte)_payloadlength;

            return buffer;
        }
    }
}
