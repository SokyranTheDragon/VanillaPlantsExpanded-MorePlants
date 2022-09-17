﻿using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace VanillaPlantsExpandedMorePlants
{
	public class WorkGiver_GrowerSowSandy : WorkGiver_GrowerSandy
	{
		protected static string CantSowCavePlantBecauseOfLightTrans;

		protected static string CantSowCavePlantBecauseUnroofedTrans;

		public override PathEndMode PathEndMode
		{
			get
			{
				return PathEndMode.ClosestTouch;
			}
		}

		public static void ResetStaticData()
		{
			CantSowCavePlantBecauseOfLightTrans = "CantSowCavePlantBecauseOfLight".Translate();
			CantSowCavePlantBecauseUnroofedTrans = "CantSowCavePlantBecauseUnroofed".Translate();
		}

		protected override bool ExtraRequirements(IPlantToGrowSettable settable, Pawn pawn)
		{
			if (!settable.CanAcceptSowNow())
			{
				return false;
			}
			Zone_GrowingSandy zone_Growing = settable as Zone_GrowingSandy;
			IntVec3 c;
			if (zone_Growing != null)
			{
				if (!zone_Growing.allowSow)
				{
					return false;
				}
				c = zone_Growing.Cells[0];
			}
			else
			{
				c = ((Thing)settable).Position;
			}
			WorkGiver_GrowerSandy.wantedPlantDef = WorkGiver_GrowerSandy.CalculateWantedPlantDef(c, pawn.Map);
			return WorkGiver_GrowerSandy.wantedPlantDef != null;
		}

		public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
		{
			Map map = pawn.Map;
			if (c.IsForbidden(pawn))
			{
				return null;
			}
			if (!PlantUtility.GrowthSeasonNow(c, map, true))
			{
				return null;
			}
			if (WorkGiver_GrowerSandy.wantedPlantDef == null)
			{
				WorkGiver_GrowerSandy.wantedPlantDef = WorkGiver_GrowerSandy.CalculateWantedPlantDef(c, map);
				if (WorkGiver_GrowerSandy.wantedPlantDef == null)
				{
					return null;
				}
			}
			List<Thing> thingList = c.GetThingList(map);
			Zone_GrowingSandy zone_Growing = c.GetZone(map) as Zone_GrowingSandy;
			bool flag = false;
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (thing.def == WorkGiver_GrowerSandy.wantedPlantDef)
				{
					return null;
				}
				if ((thing is Blueprint || thing is Frame) && thing.Faction == pawn.Faction)
				{
					flag = true;
				}
			}
			if (flag)
			{
				Thing edifice = c.GetEdifice(map);
				if (edifice == null || edifice.def.fertility < 0f)
				{
					return null;
				}
			}
			if (WorkGiver_GrowerSandy.wantedPlantDef.plant.cavePlant)
			{
				if (!c.Roofed(map))
				{
					JobFailReason.Is(CantSowCavePlantBecauseUnroofedTrans);
					return null;
				}
				if (map.glowGrid.GameGlowAt(c, ignoreCavePlants: true) > 0f)
				{
					JobFailReason.Is(CantSowCavePlantBecauseOfLightTrans);
					return null;
				}
			}
			if (WorkGiver_GrowerSandy.wantedPlantDef.plant.interferesWithRoof && c.Roofed(pawn.Map))
			{
				return null;
			}
			Plant plant = c.GetPlant(map);
			if (plant != null && plant.def.plant.blockAdjacentSow)
			{
				if (!pawn.CanReserve(plant, 1, -1, null, forced) || plant.IsForbidden(pawn))
				{
					return null;
				}
				if (zone_Growing != null && !zone_Growing.allowCut)
				{
					return null;
				}
				if (!PlantUtility.PawnWillingToCutPlant_Job(plant, pawn))
				{
					return null;
				}
				return JobMaker.MakeJob(JobDefOf.CutPlant, plant);
			}
			Thing thing2 = PlantUtility.AdjacentSowBlocker(WorkGiver_GrowerSandy.wantedPlantDef, c, map);
			if (thing2 != null)
			{
				Plant plant2 = thing2 as Plant;
				if (plant2 != null && pawn.CanReserveAndReach(plant2, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced) && !plant2.IsForbidden(pawn))
				{
					IPlantToGrowSettable plantToGrowSettable = plant2.Position.GetPlantToGrowSettable(plant2.Map);
					if (plantToGrowSettable == null || plantToGrowSettable.GetPlantDefToGrow() != plant2.def)
					{
						Zone_GrowingSandy zone_Growing2 = c.GetZone(map) as Zone_GrowingSandy;
						Zone_GrowingSandy zone_Growing3 = plant2.Position.GetZone(map) as Zone_GrowingSandy;
						if ((zone_Growing2 != null && !zone_Growing2.allowCut) || (zone_Growing3 != null && !zone_Growing3.allowCut))
						{
							return null;
						}
						if (!PlantUtility.PawnWillingToCutPlant_Job(plant2, pawn))
						{
							return null;
						}
						return JobMaker.MakeJob(JobDefOf.CutPlant, plant2);
					}
				}
				return null;
			}
			if (WorkGiver_GrowerSandy.wantedPlantDef.plant.sowMinSkill > 0 && pawn.skills != null && pawn.skills.GetSkill(SkillDefOf.Plants).Level < WorkGiver_GrowerSandy.wantedPlantDef.plant.sowMinSkill)
			{
				JobFailReason.Is("UnderAllowedSkill".Translate(WorkGiver_GrowerSandy.wantedPlantDef.plant.sowMinSkill), def.label);
				return null;
			}
			for (int j = 0; j < thingList.Count; j++)
			{
				Thing thing3 = thingList[j];
				if (!thing3.def.BlocksPlanting())
				{
					continue;
				}
				if (!pawn.CanReserve(thing3, 1, -1, null, forced))
				{
					return null;
				}
				if (thing3.def.category == ThingCategory.Plant)
				{
					if (!thing3.IsForbidden(pawn))
					{
						if (zone_Growing != null && !zone_Growing.allowCut)
						{
							return null;
						}
						if (!PlantUtility.PawnWillingToCutPlant_Job(thing3, pawn))
						{
							return null;
						}
						return JobMaker.MakeJob(JobDefOf.CutPlant, thing3);
					}
					return null;
				}
				if (thing3.def.EverHaulable)
				{
					return HaulAIUtility.HaulAsideJobFor(pawn, thing3);
				}
				return null;
			}
			if (!WorkGiver_GrowerSandy.wantedPlantDef.CanNowPlantAt(c, map) || !PlantUtility.GrowthSeasonNow(c, map, forSowing: true) || !pawn.CanReserve(c, 1, -1, null, forced))
			{
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Sow, c);
			job.plantDefToSow = WorkGiver_GrowerSandy.wantedPlantDef;
			return job;
		}
	}
}