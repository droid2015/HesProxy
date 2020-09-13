using System;
namespace ProxyServer.Client
{
    public class NguoiVanHanh
    {
        public int id;
        public string username;
        public decimal lat;
        public decimal lon;

        public NguoiVanHanh(int _id, string _username,decimal _lat,decimal _lon)
        {
            id = _id;
            username = _username;
            lat = _lat;
            lon = _lon;
        }
        public void Update()
        {
            
        }
        private void Move(decimal _lat,decimal _lon)
        {
          
            //ServerSend.PlayerPosition(this);
            //ServerSend.PlayerRotation(this);
        }
    }
}
