using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketsServer.Model
{
    public enum MSGType
    {
        user=1,
        msg
    }
    public enum MSGAction
    {
        login=1,
        logout,
        sendmsg
    }
    public class MessageBase
    {
        /// <summary>
        /// 发送者
        /// </summary>
        public string Auth { get; set; }
        /// <summary>
        /// 类型
        /// 1,user:登录/离开 login/logout
        /// 2,message:发送的消息
        /// </summary>
        public MSGType Type { get; set; }
        /// <summary>
        /// 消息内容
        /// type=user:meesage为昵称
        /// type=message：message为消息内容
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 执行的操作：
        /// 1，登录 login
        /// 2，离开 logout
        /// 3，消息 sendmsg
        /// </summary>
        public MSGAction Action { get; set; }
        /// <summary>
        /// 操作时间
        /// </summary>
        public string SendTime { get; set; }
        /// <summary>
        /// 状态：登录成功/失败
        /// </summary>
        public bool State { get; set; }

        public MessageBase()
        {
            this.SendTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            this.State = true;
        }
    }
}
