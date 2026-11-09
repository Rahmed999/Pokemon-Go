using System;
namespace WildlifeExplorer
{
    public readonly struct ZoneFix
    {
        public readonly double Latitude, Longitude, Accuracy, Timestamp;
        public ZoneFix(double lat,double lon,double accuracy,double timestamp){Latitude=lat;Longitude=lon;Accuracy=accuracy;Timestamp=timestamp;}
    }
    public enum ZoneTransition { None, Entered, Exited, Rejected }
    public sealed class ZoneRules
    {
        public bool Inside {get;private set;}
        public int Confirmations {get;private set;}
        public double Distance {get;private set;}
        public ZoneFix LastFix {get;private set;}
        readonly double latitude,longitude,entry,exit,maximumAccuracy;
        readonly int required;
        double lastTimestamp = -1;
        bool valid;
        public ZoneRules(double lat,double lon,double enter,double leave,int count,double accuracy=20)
        {
            if(!Finite(lat)||!Finite(lon)||Math.Abs(lat)>90||Math.Abs(lon)>180||!Finite(enter)||!Finite(leave)||!Finite(accuracy)||enter<=0||leave<=enter||count<1||accuracy<=0)
                throw new ArgumentOutOfRangeException("Invalid exploration zone configuration.");
            latitude=lat;longitude=lon;entry=enter;exit=leave;required=count;maximumAccuracy=accuracy;
        }
        public ZoneTransition Process(ZoneFix fix,double now)
        {
            if(!Finite(fix.Latitude)||!Finite(fix.Longitude)||!Finite(fix.Accuracy)||!Finite(fix.Timestamp)||
                Math.Abs(fix.Latitude)>90||Math.Abs(fix.Longitude)>180||fix.Timestamp<=0||now-fix.Timestamp>15||fix.Timestamp-now>2)
            { valid=false;Confirmations=0;return ZoneTransition.Rejected; }
            if(fix.Timestamp<=lastTimestamp)return ZoneTransition.None;
            if(lastTimestamp>0&&fix.Timestamp-lastTimestamp>15)Confirmations=0;
            lastTimestamp=fix.Timestamp;LastFix=fix;
            Distance=DistanceMetres(latitude,longitude,fix.Latitude,fix.Longitude);
            valid=fix.Accuracy>0&&fix.Accuracy<=maximumAccuracy;
            if(!valid){Confirmations=0;return ZoneTransition.Rejected;}
            bool crossing=Inside?Distance>exit+0.000001:Distance<=entry+0.000001;
            if(!crossing){Confirmations=0;return ZoneTransition.None;}
            if(++Confirmations<required)return ZoneTransition.None;
            Confirmations=0;Inside=!Inside;
            return Inside?ZoneTransition.Entered:ZoneTransition.Exited;
        }
        public bool HasFreshFix(double now)=>valid&&now-lastTimestamp<=15&&now-lastTimestamp>=-2;
        public void BreakConfirmation(){Confirmations=0;}
        static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
        public static double DistanceMetres(double lat1,double lon1,double lat2,double lon2)
        {
            const double radians=Math.PI/180;
            double dLat=(lat2-lat1)*radians,dLon=(lon2-lon1)*radians;
            double a=Math.Pow(Math.Sin(dLat/2),2)+Math.Cos(lat1*radians)*Math.Cos(lat2*radians)*Math.Pow(Math.Sin(dLon/2),2);
            a=Math.Max(0,Math.Min(1,a));
            return 6371000*2*Math.Atan2(Math.Sqrt(a),Math.Sqrt(1-a));
        }
    }
    public sealed class EncounterState
    {
        public bool Active {get;private set;}
        public bool WaitingForExit {get;private set;}
        public bool CanPlace(bool inside,bool fresh)=>inside&&fresh&&!Active&&!WaitingForExit;
        public void Placed(){Active=true;}
        public bool Complete(bool inside)
        {
            if(!Active)return false;
            Active=false;WaitingForExit=inside;return true;
        }
        public void Exited(){WaitingForExit=false;}
    }
}
