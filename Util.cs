using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketsServer.Util
{
    class Utils
    {
        /// <summary>  
        /// 过滤特殊字符  
        /// </summary>  
        /// <param name="s"></param>  
        /// <returns></returns>  
        public static string String2Json(String s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s.ToCharArray()[i];
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\""); break;
                    case '\\':
                        sb.Append("\\\\"); break;
                    case '/':
                        sb.Append("\\/"); break;
                    case '\b':
                        sb.Append("\\b"); break;
                    case '\f':
                        sb.Append("\\f"); break;
                    case '\n':
                        sb.Append("\\n"); break;
                    case '\r':
                        sb.Append("\\r"); break;
                    case '\t':
                        sb.Append("\\t"); break;
                    default:
                        if ((c >= 0 && c <= 31) || c == 127)//在ASCⅡ码中，第0～31号及第127号(共33个)是控制字符或通讯专用字符
                        {
                            if (c==92)// \
                            {
                                sb.Append(@"\\");
                            }
                            if (c == 39)// '
                            {
                                sb.Append(@"\'");
                            }
                            if (c == 34)// "
                            {
                                sb.Append(@"\""");
                            }
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
