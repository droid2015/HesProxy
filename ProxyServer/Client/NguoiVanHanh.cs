using System;
namespace ProxyServer.Client
{
    public class NguoiVanHanh
    {
        public int id;
        public string username;
        public float lat;
        public float lon;

        public NguoiVanHanh(int _id, string _username,float _lat,float _lon)
        {
            id = _id;
            username = _username;
            lat = _lat;
            lon = _lon;
        }
        public void Update()
        {
            
        }
        private void Move(float _lat,float _lon)
        {
          
            //ServerSend.PlayerPosition(this);
            //ServerSend.PlayerRotation(this);
        }
        public void SetInput(float _lat,float _lon)
        {
            lat = _lat;
            lon = _lon;
        }
    }
}
