using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static FishyFishy.Helpers.FishyUtils;

namespace FishyFishy.Primitive
{
	public delegate void SetEffectsParameterDelegate(Effect effect, float dissaparition);

	public enum DrawhookLayer
	{
		BehindTiles,
		AboveTiles,
		AboveNPCs,
		AbovePlayer
	}

	// Made after seeing spirit's interesting way to deal with lingering trails
	public class GhostTrail
	{
		public bool Dead { get; set; } = false;
		public Entity AttachedEntity { get; set; } = null;
		public int TrailMaxLenght { get; set; } = 9999;
		public bool ShrinkTrailLenght { get; set; } = false;
		public bool ShrinkTrailWidth { get; set; } = true;
		public bool FadeTrailOpacity { get; set; } = true;
		public float ShrinkTime { get; set; } = 1f;
		public DrawhookLayer DrawLayer { get; set; } = DrawhookLayer.AboveTiles;

		internal PrimitiveTrail trail;
		internal float timeLeft = 1f;
		internal List<Vector2> cache;

		private TrailWidthFunction realWidthFunction;
		private TrailColorFunction realColorFunction;
		private TrailPointRetrievalFunction pointRetrievalFunction;
		private SetEffectsParameterDelegate effectParametersDelegate;

		public virtual float ShrinkingWidthFunction(float completion)
		{
			float baseWidth = realWidthFunction(completion);
			if (ShrinkTrailWidth)
				baseWidth *= timeLeft;
			return baseWidth;
		}

		public virtual Color FadingColorFunction(float completion)
		{
			Color baseColor = realColorFunction(completion);
			if (FadeTrailOpacity)
				baseColor *= timeLeft;
			return baseColor;
		}

		internal void NoEffectParameters(Effect effect, float completion) { }

		public GhostTrail(List<Vector2> trailCache, PrimitiveTrail trailToClone, float duration = 0.3f, Entity attachedEntity = null, SetEffectsParameterDelegate effectParams = null, TrailPointRetrievalFunction pointRetrieval = null, int? maxPoints = null)
		{
			if (maxPoints == null)
				maxPoints = trailCache.Count;
			if (effectParams == null)
				effectParams = NoEffectParameters;
			if (pointRetrieval == null)
				pointRetrieval = RigidPointRetreivalFunction;

			cache = trailCache;
			realColorFunction = trailToClone.trailColorFunction;
			realWidthFunction = trailToClone.trailWidthFunction;
			trail = new PrimitiveTrail(trailToClone.maxPointCount, ShrinkingWidthFunction, FadingColorFunction, trailToClone.tip);
			trail.SetPositionsSmart(trailCache, attachedEntity != null ? attachedEntity.Center : Vector2.Zero, pointRetrieval);

			TrailMaxLenght = maxPoints.Value;
			ShrinkTime = duration;

			pointRetrievalFunction = pointRetrieval;
			AttachedEntity = attachedEntity;
			effectParametersDelegate = effectParams;
		}

		public void Decay()
		{
			//Shrink the trail along its time
			if (timeLeft > 0)
				timeLeft -= 1f / (60f * ShrinkTime);
			else
			{
				Dead = true;
				timeLeft = 0f;
				return;
			}

			//Clears the attached entity in case it expires
			if (AttachedEntity != null && !AttachedEntity.active)
				AttachedEntity = null;

			//Update the trail's position based on the entity it is attached to
			if (AttachedEntity != null)
				cache.Add(AttachedEntity.Center);

			//Keep the trail at the max size (Alternatively, make it shrink)
			while (cache.Count > TrailMaxLenght)
				cache.RemoveAt(0);
			if (ShrinkTrailLenght && cache.Count > 2)
				cache.RemoveAt(0);

			trail.SetPositionsSmart(cache, AttachedEntity != null ? AttachedEntity.Center : Vector2.Zero, pointRetrievalFunction);
			if (AttachedEntity != null)
				trail.NextPosition = AttachedEntity.Center + AttachedEntity.velocity;
		}

		/// <summary>
		/// How the trail is drawn to the screen
		/// </summary>
		public virtual void Draw()
		{
			Effect effect = null;
			if (effectParametersDelegate != null)
			{
				effect = FishyFishy.StreakyTrailEffect;
				effectParametersDelegate(effect, timeLeft);
			}

			trail?.Render(effect, -Main.screenPosition);
		}
	}
}
