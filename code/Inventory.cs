using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Hidden
{
	public class Inventory
	{
		public HiddenPlayer Player { get; private set; }

		public Inventory( HiddenPlayer player )
		{
			Player = player;
		}

		public List<Entity> List { get; private set; } = new();
		public Entity Active
		{
			get
			{
				return Player?.ActiveChild;
			}
			set
			{
				Player.ActiveChild = value;
			}
		}

		public bool CanAdd( Entity entity )
		{
			if ( entity is Weapon weapon && weapon.CanCarry( Player ) )
				return true;

			return false;
		}

		public void DeleteContents()
		{
			Game.AssertServer();

			foreach ( var item in List.ToArray() )
			{
				item.Delete();
			}

			List.Clear();
		}

		public Entity GetSlot( int i )
		{
			if ( List.Count <= i ) return null;
			if ( i < 0 ) return null;

			return List[i];
		}

		public int Count() => List.Count;

		public int GetActiveSlot()
		{
			var active = Active;
			var count = Count();

			for ( int i = 0; i < count; i++ )
			{
				if ( List[i] == active )
					return i;
			}

			return -1;
		}

		public void OnChildAdded( Entity child )
		{
			if ( !CanAdd( child ) )
				return;

			if ( List.Contains( child ) )
				throw new Exception( "Trying to add to inventory multiple times. This is gated by Entity:OnChildAdded and should never happen!" );

			List.Add( child );
		}

		public void OnChildRemoved( Entity child )
		{
			if ( List.Remove( child ) )
			{

			}
		}

		public bool SetActiveSlot( int i, bool evenIfEmpty = false )
		{
			var entity = GetSlot( i );
			if ( Active == entity )
				return false;

			if ( !evenIfEmpty && entity == null )
				return false;

			Active = entity;
			return entity.IsValid();
		}

		public bool SwitchActiveSlot( int idelta, bool loop )
		{
			var count = Count();
			if ( count == 0 ) return false;

			var slot = GetActiveSlot();
			var nextSlot = slot + idelta;

			if ( loop )
			{
				while ( nextSlot < 0 ) nextSlot += count;
				while ( nextSlot >= count ) nextSlot -= count;
			}
			else
			{
				if ( nextSlot < 0 ) return false;
				if ( nextSlot >= count ) return false;
			}

			return SetActiveSlot( nextSlot, false );
		}

		public Entity DropActive()
		{
			if ( !Game.IsServer ) return null;

			var active = Active;
			if ( active == null ) return null;

			if ( Drop( active ) )
			{
				Active = null;
				return active;
			}

			return null;
		}

		public bool Drop( Entity entity )
		{
			if ( !Game.IsServer )
				return false;

			if ( !Contains( entity ) )
				return false;

			entity.Parent = null;

			if ( entity is Weapon weapon )
			{
				weapon.OnCarryDrop( Player );
			}

			return true;
		}

		public bool Contains( Entity entity )
		{
			return List.Contains( entity );
		}

		public virtual bool SetActive( Entity entity )
		{
			if ( Active == entity ) return false;
			if ( !Contains( entity ) ) return false;
			Active = entity;
			return true;
		}

		public bool Add( Entity entity, bool makeActive = false )
		{
			Game.AssertServer();

			var weapon = entity as Weapon;

			if ( weapon.IsValid() && IsCarryingType( entity.GetType() ) )
			{
				var ammo = weapon.AmmoClip;
				var ammoType = weapon.Config.AmmoType;

				if ( ammo > 0 )
				{
					Player.GiveAmmo( ammoType, ammo );
				}

				entity.Delete();
				return false;
			}

			if ( entity.Owner.IsValid() )
				return false;

			if ( !CanAdd( entity ) )
				return false;

			if ( !weapon.IsValid() )
				return false;

			if ( !weapon.CanCarry( Player ) )
				return false;

			entity.Parent = Player;
			weapon.OnCarryStart( Player );

			if ( makeActive )
			{
				SetActive( entity );
			}

			return true;
		}

		public bool IsCarryingType( Type t )
		{
			return List.Any( x => x.GetType() == t );
		}
	}
}
