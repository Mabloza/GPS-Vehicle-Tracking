using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapPoint;

namespace SMS
{
    class MapUtils
    {

        smartrack device;
        Form1 mainFrm;

        public MapUtils(smartrack device, Form1 mainFrm)
        {
            this.device = device;
            this.mainFrm = mainFrm;
        }

        public void radiusSearch(MapPoint.Map map,double lat, double lon, double lat_vei, double lon_vei)
        {
            //center of search Radius
            Location origin = map.GetLocation(lat,lon,map.Altitude);
            //var origin = new VELatLong(43.645, -79.389);
            //search radius in km
            int radius = int.Parse(mainFrm.RADIUS_BERSAGLIO);
            //for (var i = 0; i < dataLayer.GetShapeCount(); i++)         //controllo per tutta la flotta
            //{
                Location latlong = map.GetLocation(lat_vei,lon_vei,map.Altitude);
                double d = distance(origin, latlong);
                if (Math.Abs(d) <= Math.Abs(radius))
                {
                    drawCircle(origin, radius, map);
                    map.GoToLatLong(origin.Latitude, origin.Longitude, map.Altitude);
                    mainFrm.frmAllarm.addNewAllarm(DateTime.Now.ToString(), device.device_number, "Terminale presso obiettivo");
                }
            //}
            
        }

        public double distance(Location latlong,Location latlong2)
        {
          double lat1 = latlong.Latitude;
          double lon1 = latlong.Longitude;
          double lat2 = latlong2.Latitude;
          double lon2 = latlong2.Longitude;
          double earthRadius = 6371; //appxoximate radius in miles
              
          double factor = Math.PI/180;
          double dLat = (lat2-lat1)*factor;
          double dLon = (lon2-lon1)*factor; 
          double a = Math.Sin(dLat/2) * Math.Sin(dLat/2) + Math.Cos(lat1*factor) * Math.Cos(lat2*factor) * Math.Sin(dLon/2) * Math.Sin(dLon/2); 
          double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a)); 
          double d = earthRadius * c;
          return d;
        }

        public void drawCircle(Location origin,double radius, MapPoint.Map map)
        {  
          map.Shapes.AddShape(GeoAutoShapeType.geoShapeOval,origin,radius,radius);
        }  
    }
}
