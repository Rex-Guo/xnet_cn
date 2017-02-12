using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace youdao翻译api
{
    //[Serializable]
    [DataContract]   
    public class Youdao_option
    {
        //   {"translation":["是предназначенн类下载HTTP服务器响应"],"query":"Представляет класс, предназначеннный для загрузки ответа от HTTP-сервера","errorCode":0}
        //    errorCode":0
        //"query":"good",
        //"translation":["好"], // 有道翻译
        //"basic":

            public Youdao_option()
        {
            //translation=new string[] { "默认是空" };
        }

        [DataMember]
        public int errorCode { get; set; }
        [DataMember]
        public string query { get; set; }
        //private string[] _trajslation;
        [DataMember (EmitDefaultValue =true,IsRequired =false)]
        public string[] translation { get  ; set  ; }
        ////[DataMember]
        [DataMember(IsRequired = false)]
        public object basic { get; set; }
    }
}
