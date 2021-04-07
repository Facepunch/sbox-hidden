using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
    public abstract partial class BaseRound : NetworkClass
	{
		public virtual int RoundDuration => 0;
		public virtual string RoundName => "";

		public List<Player> Players = new();

		public float RoundEndTime { get; set; }

		public float TimeLeft
		{
			get
			{
				return RoundEndTime - Sandbox.Time.Now;
			}
		}

		// TODO: This can be done better by using a shared timestamp.
		[NetPredicted]
		public string TimeLeftFormatted { get; set; }

		public void Start()
		{
			if ( Host.IsServer && RoundDuration > 0 )
			{
				RoundEndTime = Sandbox.Time.Now + RoundDuration;
			}
			
			OnStart();
		}

		public void Finish()
		{
			if ( Host.IsServer )
			{
				RoundEndTime = 0f;
				Players.Clear();
			}

			OnFinish();
		}

		public void AddPlayer( Player player )
		{
			Host.AssertServer();

			if ( !Players.Contains(player) )
			{
				Players.Add( player );
			}
		}

		public virtual void OnPlayerSpawn( Player player ) { }

		public virtual void OnPlayerKilled( Player player ) { }

		public virtual void OnPlayerLeave( Player player )
		{
			Players.Remove( player );
		}

		public virtual void OnSecond()
		{
			if ( Host.IsServer )
			{
				if ( RoundEndTime > 0 && Sandbox.Time.Now >= RoundEndTime )
				{
					RoundEndTime = 0f;
					OnTimeUp();
				}
				else
				{
					TimeLeftFormatted = TimeSpan.FromSeconds( TimeLeft ).ToString( @"mm\:ss" );
				}
			}
		}

		protected virtual void OnStart() { }

		protected virtual void OnFinish() { }

		protected virtual void OnTimeUp() { }
	}
}
