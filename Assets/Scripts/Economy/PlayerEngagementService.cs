using System;

namespace Scripts.Economy
{
    public enum PlayerActivity { ActiveDay,SingleStarted,DuelStarted,SinglePlayed,DuelPlayed }
    // Five last-day checkpoints, rather than an ever-growing daily receipt list.
    public sealed class PlayerEngagementData
    {
        public string ConsentScope;
        public int ActiveDay=-1,SingleStartedDay=-1,DuelStartedDay=-1,SinglePlayedDay=-1,DuelPlayedDay=-1;
        public int Get(PlayerActivity activity)=>activity==PlayerActivity.ActiveDay?ActiveDay:activity==PlayerActivity.SingleStarted?SingleStartedDay:
            activity==PlayerActivity.DuelStarted?DuelStartedDay:activity==PlayerActivity.SinglePlayed?SinglePlayedDay:DuelPlayedDay;
        public void Set(PlayerActivity activity,int day)
        {
            if(activity==PlayerActivity.ActiveDay)ActiveDay=day;
            else if(activity==PlayerActivity.SingleStarted)SingleStartedDay=day;
            else if(activity==PlayerActivity.DuelStarted)DuelStartedDay=day;
            else if(activity==PlayerActivity.SinglePlayed)SinglePlayedDay=day;
            else DuelPlayedDay=day;
        }
    }
    public sealed class PlayerEngagementService
    {
        private readonly IPlayerProfileRepository _profiles;
        private readonly Func<EconomyTelemetryContext> _context;
        private string _scope;
        private int _day=-1,_recorded;
        public PlayerEngagementService(IPlayerProfileRepository profiles,Func<EconomyTelemetryContext> context){_profiles=profiles;_context=context;}
        public void Record(PlayerActivity activity)
        {
            if(!Enum.IsDefined(typeof(PlayerActivity),activity))throw new ArgumentOutOfRangeException(nameof(activity));
            var context=_context();if(context==null)return;
            if(_scope!=context.ConsentScope || _day!=context.UtcDay){_scope=context.ConsentScope;_day=context.UtcDay;_recorded=0;}
            int bit=1<<(int)activity;if((_recorded&bit)!=0)return;
            _profiles.Update(profile=>{
                if(profile.TelemetryEngagement.ConsentScope!=_scope)profile.TelemetryEngagement=new PlayerEngagementData {ConsentScope=_scope};
                if(profile.TelemetryEngagement.Get(activity)<_day)profile.TelemetryEngagement.Set(activity,_day);
            });
            _recorded|=bit;
        }
        public static string Name(PlayerActivity activity)=>activity==PlayerActivity.ActiveDay?"active_day":
            activity==PlayerActivity.SingleStarted?"single_started_day":activity==PlayerActivity.DuelStarted?"duel_started_day":
            activity==PlayerActivity.SinglePlayed?"single_played_day":"duel_played_day";
    }

    public sealed class ProfileTelemetryDelivery
    {
        private readonly IPlayerProfileRepository _profiles;
        public ProfileTelemetryDelivery(IPlayerProfileRepository profiles){_profiles=profiles;}
        // Accepted by the SDK does not mean received by the server. Keep a stable
        // event id when a crash occurs between SDK acceptance and local ack.
        public int Drain(string consentScope,Func<EconomyTelemetryEvent,bool> send,int maximum=16)
        {
            if(string.IsNullOrEmpty(consentScope) || maximum<=0)return 0;
            var acknowledged=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,EconomyTelemetryEvent>>();
            int accepted=0;
            foreach(var entry in _profiles.Snapshot().TelemetryOutbox)
            {
                if(acknowledged.Count>=maximum)break;
                if(entry.Value.ConsentScope==consentScope)
                {
                    if(!send(entry.Value))break;
                    accepted++;
                }
                acknowledged.Add(entry);
            }
            if(acknowledged.Count>0)_profiles.Update(profile=>{
                foreach(var entry in acknowledged)
                    if(profile.TelemetryOutbox.TryGetValue(entry.Key,out var current) && current.EventId==entry.Value.EventId)profile.TelemetryOutbox.Remove(entry.Key);
            });
            return accepted;
        }
    }
}
