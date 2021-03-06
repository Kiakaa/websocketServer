﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocketServer
{
    public class DataFrame
    {
        DataFrameHeader _header;
        private byte[] _extend = new byte[0];
        private byte[] _mask = new byte[0];
        private byte[] _content = new byte[0];

        //RFC6455 5.2基本帧协议
        public DataFrame(byte[] buffer)
        {
            //帧头
            _header = new DataFrameHeader(buffer);

            //扩展长度：if payload len==126/127) Extended payload length。第3、4字节
            if (_header.Length == 126)
            {
                _extend = new byte[2];
                Buffer.BlockCopy(buffer, 2, _extend, 0, 2);
            }
            else if (_header.Length == 127)
            {
                _extend = new byte[8];
                Buffer.BlockCopy(buffer, 2, _extend, 0, 8);
            }

            //是否有掩码
            if (_header.HasMask)
            {
                _mask = new byte[4];
                Buffer.BlockCopy(buffer, _extend.Length + 2, _mask, 0, 4);
            }

            //消息体。
            //负载长度=“扩展数据”长度+“应用数据”长度
            //“应用数据”的长度 = 负载长度减去“扩展数据”长度。
            if (_extend.Length == 0)
            {
                _content = new byte[_header.Length];
                Buffer.BlockCopy(buffer, _extend.Length + _mask.Length + 2 , _content, 0, _content.Length);
            }
            else if (_extend.Length == 2)
            {
                int contentLength = (int)_extend[0] * 256 + (int)_extend[1];
                _content = new byte[contentLength];
                Buffer.BlockCopy(buffer, _extend.Length + _mask.Length + 2, _content, 0, contentLength > 1024 * 100 ? 1024 * 100 : contentLength);
            }
            else
            {
                long len = 0;
                int n = 1;
                for (int i = 7; i >= 0; i--)
                {
                    len += (int)_extend[i] * n;
                    n *= 256;
                }
                _content = new byte[len];
                Buffer.BlockCopy(buffer, _extend.Length + _mask.Length + 2, _content, 0, _content.Length);
            }

            if (_header.HasMask) _content = GetDecodedPayloadData(_content, _mask);

        }
        //Payload length: 7 bits, 7+16 bits, 或者 7+64 bits
        public DataFrame(string content)
        {
            _content = Encoding.UTF8.GetBytes(content);
            int length = _content.Length;
            
            if (length < 126)
            {
                _extend = new byte[0];
                _header = new DataFrameHeader(true, false, false, false, 1, false, length);
            }
            else if (length < 65536)
            {
                _extend = new byte[2];
                _header = new DataFrameHeader(true, false, false, false, 1, false, 126);
                _extend[0] = (byte)(length / 256);
                _extend[1] = (byte)(length % 256);
            }
            else
            {
                _extend = new byte[8];
                _header = new DataFrameHeader(true, false, false, false, 1, false, 127);

                int left = length;
                int unit = 256;

                for (int i = 7; i > 1; i--)
                {
                    _extend[i] = (byte)(left % unit);
                    left = left / unit;

                    if (left == 0)
                        break;
                }
            }
        }

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[2 + _extend.Length + _mask.Length + _content.Length];
            Buffer.BlockCopy(_header.GetBytes(), 0, buffer, 0, 2);
            Buffer.BlockCopy(_extend, 0, buffer, 2, _extend.Length);
            Buffer.BlockCopy(_mask, 0, buffer, 2 + _extend.Length, _mask.Length);
            Buffer.BlockCopy(_content, 0, buffer, 2 + _extend.Length + _mask.Length, _content.Length);
            return buffer;
        }
        
        public string Text 
        { 
            get 
            {
                if (_header.OpCode != 1)
                    return string.Empty;

                return Encoding.UTF8.GetString(_content); 
            } 
        }

        /// <summary>
        /// 根据payloadData、mask 解码：5.3.客户端到服务器掩码
        /// </summary>
        /// <param name="payloadData">payloadData：字节数组。承载的数据</param>
        /// <param name="mask">掩码 4字节：字节数组。掩码</param>
        /// <returns></returns>
        private byte[] GetDecodedPayloadData(byte[] payloadData, byte[] mask)
        {
            byte[] DecodedPayloadData = new byte[payloadData.Length];
            for (var i = 0; i < payloadData.Length; i++)
            {
                DecodedPayloadData[i] = (byte)(payloadData[i] ^ mask[i % 4]);
            }
            return DecodedPayloadData;
        }

    }
}
