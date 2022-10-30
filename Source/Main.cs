using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PavonisInteractive.TerraInvicta;
using UnityModManagerNet;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace SolarInvicta
{
	internal static class Main
	{
		//Load Unity Mod Manager
		private static bool Load(UnityModManager.ModEntry modEntry)
		{
			new Harmony(modEntry.Info.Id).PatchAll(Assembly.GetExecutingAssembly());
			Main.mod = modEntry;
			modEntry.OnToggle = new Func<UnityModManager.ModEntry, bool, bool>(Main.OnToggle);
			return true;
		}

		private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
		{
			Main.enabled = value;
			return true;
		}

		public static bool enabled;

		public static UnityModManager.ModEntry mod;

		//Fixed CTD caused by targeting. Allow councilor to reach target when they are in the same spacebody, skip the original method
		[HarmonyPatch(typeof(TIMissionCondition_TargetInRange), "CanTarget")]
		private class TIMissionCondition_TargetInRange_CanTarget_Patch
		{
			private static bool Prefix(TIMissionCondition __instance, ref string __result, TICouncilorState councilor, TIGameState possibleTarget)
			{
				if (councilor.ref_spaceBody == possibleTarget.ref_spaceBody && ((councilor.OnEarth && possibleTarget.ref_orbit==null)||(!councilor.OnEarth && possibleTarget.ref_orbit!=null)))
				{
					if (possibleTarget.isCouncilorState)
					{
						TIGameState tigameState = TIMissionPhaseState.CouncilorLastKnownLocation(possibleTarget.ref_councilor);
						if ((!(councilor.ref_hab != null) || !(councilor.ref_hab == tigameState.ref_hab)) && (!(councilor.ref_fleet != null) || !(councilor.ref_fleet == tigameState.ref_fleet)) && (!(councilor.ref_habSite != null) || !(councilor.ref_habSite == tigameState.ref_habSite)))
						{
							if (!councilor.OnEarth || !(tigameState.ref_spaceAsset == null))
							{
								__result = __instance.GetType().Name;
							}
							TISpaceBodyState ref_spaceBody = tigameState.ref_spaceBody;
							if (ref_spaceBody == null || !ref_spaceBody.isEarth)
							{
								__result = __instance.GetType().Name;
							}
						}
						if (councilor.ValidDestination(TIUtilities.ObjectToExactLocation(tigameState)))
						{
							__result = "_Pass";
							return false;
						}
					}
					else
					{
						if ((!(councilor.ref_hab != null) || !(councilor.ref_hab == possibleTarget.ref_hab)) && (!(councilor.ref_fleet != null) || !(councilor.ref_fleet == possibleTarget.ref_fleet)) && (!(councilor.ref_habSite != null) || !(councilor.ref_habSite == possibleTarget.ref_habSite)))
						{
							if (!councilor.OnEarth || !(possibleTarget.ref_spaceAsset == null))
							{
								__result = __instance.GetType().Name;
							}
							TISpaceBodyState ref_spaceBody2 = possibleTarget.ref_spaceBody;
							if (ref_spaceBody2 == null || !ref_spaceBody2.isEarth)
							{
								__result = __instance.GetType().Name;
							}
						}
						if (councilor.ValidDestination(TIUtilities.ObjectToExactLocation(possibleTarget)))
						{
							__result = "_Pass";
							return false;
						}
					}
				}
				else 
				{
					__result = __instance.GetType().Name;
				}
				return false;
			}
		}

		//Economy post patch for space nations
		[HarmonyPatch(typeof(TINationState), "economyPriorityPerCapitaIncomeChange", MethodType.Getter)]
		private class TINationState_economyPriorityPerCapitaIncomeChange_Patch
		{
			private static void Postfix(TINationState __instance, ref float __result)
			{
				bool flag = __instance.solarBody != "Earth";
				if (flag)
				{
					bool flag2 = (double)__instance.population_Millions < 0.01;
					if (flag2)
					{
						__result *= 0.5f;
					}
					else
					{
						bool flag3 = __instance.population_Millions < 10f;
						if (flag3)
						{
							__result *= 0.8f;
						}
						else
						{
							__result *= 1.1f;
						}
					}
				}
			}
		}

		//Welfare post patch for space nations
		[HarmonyPatch(typeof(TINationState), "WelfarePriorityComplete")]
		private class TINationState_WelfarePriorityComplete_Patch
		{
			private static void Postfix(TINationState __instance)
			{
				bool flag = __instance.solarBody != "Earth";
				if (flag)
				{
					bool flag2 = __instance.population_Millions < 0.01f;
					if (flag2)
					{
						foreach (TIRegionState tiregionState in __instance.regions)
						{
							tiregionState.populationInMillions += 0.01f;
						}
					}
					else
					{
						bool flag3 = __instance.population_Millions < 5f;
						if (flag3)
						{
							foreach (TIRegionState tiregionState2 in __instance.regions)
							{
								tiregionState2.populationInMillions *= 1.04f;
							}
						}
					}
				}
			}
		}

		//knowledge post patch for space nations
		[HarmonyPatch(typeof(TINationState), "knowledgePriorityEducationChange", MethodType.Getter)]
		private class TINationState_knowledgePriorityEducationChange_Patch
		{
			private static void Postfix(TINationState __instance, ref float __result)
			{
				bool flag = __instance.solarBody != "Earth";
				if (flag)
				{
					bool flag2 = __instance.population_Millions < 0.01f;
					if (flag2)
					{
						__result = TemplateManager.global.knowledgePriorityEducationIncrease * 10f;
					}
					else
					{
						bool flag3 = __instance.population_Millions < 10f;
						if (flag3)
						{
							__result = TemplateManager.global.knowledgePriorityEducationIncrease * 5f;
						}
						else
						{
							__result = TemplateManager.global.knowledgePriorityEducationIncrease * 2f;
						}
					}
				}
			}
		}

		//Boost post patch for space nations
		[HarmonyPatch(typeof(TINationState), "BoostIncrease")]
		private class TINationState_BoostIncrease_Patch
		{
			private static void Postfix(TINationState __instance, ref float __result)
			{
				bool flag = __instance.solarBody != "Earth";
				if (flag)
				{
					__result = TemplateManager.global.boostPriorityIncreaseAtEquator * TemplateManager.global.spaceResourceToTons * (10f / (float)__instance.ref_spaceBody.escapeVelocity_kps);
				}
			}
		}

		//Initial Boost post patch for space nations
		[HarmonyPatch(typeof(TINationState), "spaceflightInitialBoost", MethodType.Getter)]
		private class TINationState_spaceflightInitialBoost_Patch
		{
			private static void Postfix(TINationState __instance, ref float __result)
			{
				bool flag = __instance.solarBody != "Earth";
				if (flag)
				{
					__result = 20f;
				}
			}
		}

		//Post patch to correct population display for all spacebodies
		[HarmonyPatch(typeof(TISpaceBodyState), "population", MethodType.Getter)]
		private class TISpaceBodyState_population_Patch
		{
			private static void Postfix(TISpaceBodyState __instance, ref ulong __result)
			{
				ulong num = 0UL;
				foreach (TINationState tinationState in __instance.nations)
				{
					num += (ulong)tinationState.population;
				}
				bool isEarth = __instance.isEarth;
				if (isEarth)
				{
					__result = num;
				}
				else
				{
					__result += num;
				}
			}
		}

		//Post patch to correct population display for earth and space
		[HarmonyPatch(typeof(IntelScreenController), "RefreshGlobalTab")]
		private class IntelScreenController_RefreshGlobalTab_Patch
		{
			private static void Postfix(IntelScreenController __instance)
			{
				double num = 0.0;
				double num2 = 0.0;
				foreach (TISpaceBodyState tispaceBodyState in GameStateManager.AllSpaceBodies())
				{
					bool isEarth = tispaceBodyState.isEarth;
					if (isEarth)
					{
						num = tispaceBodyState.population;
					}
					else
					{
						num2 += tispaceBodyState.population;
					}
				}
				double num3 = GameStateManager.AllNations().Sum((TINationState x) => x.GDP);
				double num4 = num3 / (num + num2);
				__instance.globalDataData.SetText(Loc.T("UI.Intel.GlobalDataData", new object[]
				{
					num.ToString("N0"),
					num2.ToString("N0"),
					TIUtilities.FormatBigNumber(num3, 1),
					num4.ToString("N0")
				}), true);
			}
		}

		//Post patch for space marine with enum index ArmyType10	
		[HarmonyPatch(typeof(TIArmyState), "techLevel", MethodType.Getter)]
		private class TIArmyState_techLevel_Patch
		{
			private static void Postfix(TIArmyState __instance, ref float __result)
			{
				if (__instance.armyType == (ArmyType)10)
				{
					__result = __instance.homeNation.maxMilitaryTechLevel + 1f;
				}
				else
                {
					__result = __instance.homeNation.militaryTechLevel;
				}
			}
		}

		//Post patch for space marine with enum index ArmyType10
		[HarmonyPatch(typeof(TIArmyState), "NewArmy")]
		private class TIArmyState_NewArmy_Patch
		{
			private static void Postfix(TIArmyState __instance, ArmyType ___armyType)
			{
				if (___armyType == (ArmyType)10)
				{
					__instance.displayName = Loc.T("TIArmyTemplate.displayName.SpaceMarine");
					__instance.displayNameWithArticle = Loc.T("TIArmyTemplate.displayNameWithArticle.SpaceMarine");
				}
			}
		}

		//Post patch for space marine with enum index ArmyType10
		[HarmonyPatch(typeof(TIArmyState), "Disband")]
		private class TIArmyState_Disband_Patch
		{
			private static void Postfix(TIArmyState __instance)
			{
				if (__instance.armyType == (ArmyType)10)
				{
					GameStateManager.RemoveGameState<TIAlienArmyState>(__instance.ID, false);
				}
			}
		}

		//Space Marine deployment patch, skip the original method
		[HarmonyPatch(typeof(AlienLandArmyOperation), "ExecuteOperation")]
		private class AlienLandArmyOperation_ExecuteOperation_Patch
		{
			private static bool Prefix(AlienLandArmyOperation __instance, TIGameState actorState, TIGameState target)
			{
				if (__instance.ActorCanPerformOperation(actorState, target))
				{
					TISpaceFleetState ref_fleet = actorState.ref_fleet;
					TIRegionState ref_region = target.ref_region;
					TIFactionState ref_faction = actorState.ref_faction;
					TINationState ref_nation = target.ref_nation;
					if (!ref_fleet.inCombat)
					{
						foreach (TISpaceShipState tispaceShipState in ref_fleet.ships)
						{
							if (tispaceShipState.landArmyEligible)
							{
								tispaceShipState.DestroyShip(false, null);
								if (ref_faction == GameStateManager.AlienFaction())
								{
									ref_region.alienLanding.TriggerLanding();
								}
								else
								{
									TIArmyState tiarmyState = GameStateManager.CreateNewGameState<SISpaceMarineState>();
									tiarmyState.createdFromTemplate = false;
									tiarmyState.deploymentType = DeploymentType.Naval;
									if (ref_nation.executiveFaction != null && ref_nation.executiveFaction != ref_faction && ref_nation.wars.Count != 0)
									{
										int num = UnityEngine.Random.Range(0, ref_nation.wars.Count);
										TINationState tinationState = ref_nation.wars[num];
										int num2 = UnityEngine.Random.Range(0, tinationState.regions.Count);
										tiarmyState.homeRegion = tinationState.regions[num2];
										tiarmyState.NewArmy((ArmyType)10, 0, 1f);
										tinationState.AddArmy(tiarmyState);
										tiarmyState.MoveArmyToRegion(ref_region, true);
									}
									else if (ref_nation.executiveFaction != null && ref_nation.executiveFaction != ref_faction && ref_nation.rivals.Count != 0)
									{
										int num3 = UnityEngine.Random.Range(0, ref_nation.rivals.Count);
										TINationState tinationState2 = ref_nation.rivals[num3];
										int num4 = UnityEngine.Random.Range(0, tinationState2.regions.Count);
										tinationState2.DeclareLimitedWar(ref_faction, ref_nation);
										tinationState2.IsAtWarWith(ref_nation);
										tiarmyState.homeRegion = tinationState2.regions[num4];
										tiarmyState.NewArmy((ArmyType)10, 0, 1f);
										tinationState2.AddArmy(tiarmyState);
										tiarmyState.MoveArmyToRegion(ref_region, true);
									}
									else
									{
										tiarmyState.homeRegion = ref_region;
										tiarmyState.NewArmy((ArmyType)10, 0, 1f);
										ref_nation.AddArmy(tiarmyState);
										tiarmyState.MoveArmyToRegion(ref_region, true);
									}
									TINotificationQueueState.LogNewArmyBuilt(tiarmyState);
									tiarmyState.homeNation.SetDataDirty();
									TIGlobalValuesState.GlobalValues.ModifyMarketValuesForArmyPriority();
									tiarmyState.SetGameStateCreated();
									tiarmyState.faction = ref_faction;
									TINotificationQueueState.LogArmyAssignedToFaction(tiarmyState, ref_faction);
								}
								foreach (TICouncilorState ticouncilorState in tispaceShipState.councilorPassengers)
								{
									ticouncilorState.SetLocation(ref_region);
								}
								TIEffectsState.AddEffect(TemplateManager.Find<TIEffectTemplate>("Effect_ManyAliensOnEarth", false), GameStateManager.AlienFaction(), null, null);
								break;
							}
						}
					}
				}
				return false;
			}

		}

		//Space Marine display patch, skip the original method
		[HarmonyPatch(typeof(ArmyDetailController), "SetArmyMiltech")]
		private class ArmyDetailController_SetArmyMiltech_Patch
		{
			private static bool Prefix(ref TIArmyState army, ref TMP_Text textItem)
			{
				ArmyType armyType = army.armyType;
				if (armyType - ArmyType.AlienMegafauna <= 1)
				{
					textItem.SetText(army.techLevel.ToString("N1"), true);
				}
				else if (armyType == (ArmyType)10)
				{
					textItem.SetText(army.battleValue.ToString("N1"), true);
				}
                else
                {
					textItem.SetText(army.homeNation.GetMilitaryDescriptiveStringAndValue(1), true);
				}
				return false;
			}
		}

		//CTD Fixed For AI
		[HarmonyPatch(typeof(TINationState), "BestBoostLatitude", MethodType.Getter)]
		private class TINationState_BestBoostLatitude_Patch
		{
			private static bool Prefix(TINationState __instance, ref float __result)
			{
				if (__instance.solarBody == "Earth")
				{
					__result = __instance.regions.MinBy((TIRegionState x) => Mathf.Abs(x.boostLatitude)).boostLatitude;
				}
				else 
				{
					__result = 0;
				}
				return false;
			}
		}
		//Pop Fixed For SpaceRegions
		[HarmonyPatch(typeof(TIRegionState), "InitWithTemplate")]
		private class TIRegionState_InitWithTemplate_Patch
		{
			private static void Postfix(TIRegionState __instance, TIDataTemplate ___template)
			{
				TIRegionTemplate tiregionTemplate = ___template as TIRegionTemplate;
				__instance.populationInMillions = tiregionTemplate.population_Millions;			
			}
		}
		//GDP fix for mission
		[HarmonyPatch(typeof(TIMissionModifier_TargetNationGDP), "GetModifier")]
		private class TIMissionModifier_TargetNationGDP_GetModifier_Patch
		{
			private static void Postfix(ref float __result)
			{
				if (float.IsNaN(__result) || float.IsInfinity(__result))
				{
					__result = 0;
				}
			}
		}
		//Time Cost Patch for SI
		[HarmonyPatch(typeof(TISpaceObjectState), "GenericTransferTimeFromEarthsSurface_d")]
		private class TISpaceObjectState_GenericTransferTimeFromEarthsSurface_d_Patch
		{
			private static bool Prefix(ref float __result,TIFactionState faction, TIGameState destination)
			{
				float num = TISpaceObjectState.GenericTransferTime_d(faction, GameStateManager.Earth(), destination);
				if (GameStateManager.Luna().nations.Exists((TINationState nation)=>nation.ref_faction==faction&&nation.population_Millions > 5.1f))
					num = Mathf.Min(TISpaceObjectState.GenericTransferTime_d(faction, GameStateManager.Luna(), destination),num);
				if (GameStateManager.Mars().nations.Exists((TINationState nation) => nation.ref_faction == faction && nation.population_Millions > 5.1f))
					num = Mathf.Min(TISpaceObjectState.GenericTransferTime_d(faction, GameStateManager.Mars(), destination), num);
				if (GameStateManager.Ceres().nations.Exists((TINationState nation) => nation.ref_faction == faction && nation.population_Millions > 5.1f))
					num = Mathf.Min(TISpaceObjectState.GenericTransferTime_d(faction, GameStateManager.Ceres(), destination), num);
				if (GameStateManager.Mercury().nations.Exists((TINationState nation) => nation.ref_faction == faction && nation.population_Millions > 5.1f))
					num = Mathf.Min(TISpaceObjectState.GenericTransferTime_d(faction, GameStateManager.Mercury(), destination), num);
				IEnumerable<TINationState> nations = from nation in GameStateManager.AllNations()where nation.ref_region != null select nation;
				if (nations.Any((TINationState x) => (x.ref_spaceObject.GetSunOrbitingRelatedObject == GameStateManager.Jupiter() && x.ref_faction == faction && x.population_Millions > 5.1f)))
					num = Mathf.Min(TISpaceObjectState.GenericTransferTime_d(faction, GameStateManager.Jupiter(), destination), num);
				if (nations.Any((TINationState x) => (x.ref_spaceObject.GetSunOrbitingRelatedObject == GameStateManager.Saturn() && x.ref_faction == faction && x.population_Millions > 5.1f)))
					num = Mathf.Min(TISpaceObjectState.GenericTransferTime_d(faction, GameStateManager.Saturn(), destination), num);
				if (nations.Any((TINationState x) => (x.ref_spaceObject.GetSunOrbitingRelatedObject == GameStateManager.Uranus() && x.ref_faction == faction && x.population_Millions > 5.1f)))
					num = Mathf.Min(TISpaceObjectState.GenericTransferTime_d(faction, GameStateManager.Uranus(), destination), num);
				if (nations.Any((TINationState x) => (x.ref_spaceObject.GetSunOrbitingRelatedObject == GameStateManager.Neptune() && x.ref_faction == faction && x.population_Millions > 5.1f)))
					num = Mathf.Min(TISpaceObjectState.GenericTransferTime_d(faction, GameStateManager.Neptune(), destination), num);
				__result = num;
				return false;
			}
		}
		//Boost Cost Patch for SI 修正了推进的消耗
		[HarmonyPatch(typeof(TISpaceObjectState), "GenericTransferBoostFromEarthSurface")]
		private class TISpaceObjectState_GenericTransferBoostFromEarthSurface_Patch
		{
			private static bool Prefix(ref double __result, TIFactionState faction, TIGameState destination, float mass_tons)
			{
				TIOrbitState ref_orbit = destination.ref_orbit;
				if (ref_orbit != null && ref_orbit.isEarthLEO)
				{
					__result = (double)(mass_tons * TemplateManager.global.spaceResourceToTons);
				}


				double num = SIController.GenericTransferDV_mps(GameStateManager.LEOStates()[0], destination);

				if (GameStateManager.Luna().nations.Exists((TINationState nation) => nation.ref_faction == faction && nation.population_Millions > 5.1f))
					num = Mathd.Min(SIController.GenericTransferDV_mps(GameStateManager.Luna(), destination)-1700, num);
				if (GameStateManager.Mars().nations.Exists((TINationState nation) => nation.ref_faction == faction && nation.population_Millions > 5.1f))
					num = Mathd.Min(SIController.GenericTransferDV_mps(GameStateManager.Mars(), destination) - 3036, num);
				if (GameStateManager.Ceres().nations.Exists((TINationState nation) => nation.ref_faction == faction && nation.population_Millions > 5.1f))
					num = Mathd.Min(SIController.GenericTransferDV_mps(GameStateManager.Ceres(), destination) - 367, num);
				if (GameStateManager.Mercury().nations.Exists((TINationState nation) => nation.ref_faction == faction && nation.population_Millions > 5.1f))
					num = Mathd.Min(SIController.GenericTransferDV_mps(GameStateManager.Mercury(), destination) - 3036, num);
				IEnumerable<TINationState> nations = from nation in GameStateManager.AllNations() where nation.ref_region != null select nation;
				if (nations.Any((TINationState x) => (x.ref_spaceObject.GetSunOrbitingRelatedObject == GameStateManager.Jupiter() && x.ref_faction == faction && x.population_Millions > 5.1f)))
					num = Mathd.Min(SIController.GenericTransferDV_mps(GameStateManager.Jupiter(), destination)- 42500, num);
				if (nations.Any((TINationState x) => (x.ref_spaceObject.GetSunOrbitingRelatedObject == GameStateManager.Saturn() && x.ref_faction == faction && x.population_Millions > 5.1f)))
					num = Mathd.Min(SIController.GenericTransferDV_mps(GameStateManager.Saturn(), destination)- 25779, num);
				if (nations.Any((TINationState x) => (x.ref_spaceObject.GetSunOrbitingRelatedObject == GameStateManager.Uranus() && x.ref_faction == faction && x.population_Millions > 5.1f)))
					num = Mathd.Min(SIController.GenericTransferDV_mps(GameStateManager.Uranus(), destination)- 15200, num);
				if (nations.Any((TINationState x) => (x.ref_spaceObject.GetSunOrbitingRelatedObject == GameStateManager.Neptune() && x.ref_faction == faction && x.population_Millions > 5.1f)))
					num = Mathd.Min(SIController.GenericTransferDV_mps(GameStateManager.Neptune(), destination)- 16743, num);
				
				num *= 0.001;

				if (destination.ref_habSite != null)
				{
					num += destination.ref_habSite.DeltaVToLandFromInterface_kps(null, 9.8, true, true);
				}
				else if (destination.isSpaceBodyState && destination.ref_spaceBody.habSites.Length != 0)
				{
					num += destination.ref_spaceBody.habSites[0].DeltaVToLandFromInterface_kps(null, 9.8, true, true);
				}
				float num2 = 2.11f + TIEffectsState.SumEffectsModifiers(Context.GenericTransferEV_kps, faction, 2.11f);
				__result = (double)mass_tons * Mathd.Exp(num / (double)num2) * (double)TemplateManager.global.spaceResourceToTons;

				return false;
			}
		}
		//Patch MoveToTarget for councilor teleporting bugs 给干员传送打了个补丁
		[HarmonyPatch(typeof(TIMissionState), "GetInitialMissionLocation")]
		private class TIMissionState_GetInitialMissionLocation_Patch
		{
			private static bool Prefix(TIMissionState __instance, ref TIGameState __result)
			{
				if (__instance.missionTemplate.movementRule == MissionMovementRule.MoveToTarget)
				{
					__result = __instance.targetLocation;
				}
				else if (__instance.missionTemplate.movementRule == MissionMovementRule.MoveToLaunchSite)
				{
					if (__instance.councilor.OnEarth)
					{
						if (__instance.councilor.ref_spaceBody == GameStateManager.Earth())
						{
							TIRegionSpaceFacilityState tiregionSpaceFacilityState = __instance.councilor.faction.SelectRandomLaunchSite();
							__result = ((tiregionSpaceFacilityState != null) ? tiregionSpaceFacilityState.ref_region : null) ?? GameStateManager.FindByTemplate<TIRegionState>("Astana", false);
							return false;
						}
						else 
						{
							__result = __instance.councilor.ref_region;
							return false;
						}
					}
					__result = __instance.councilor.location;
				}
				else 
				{
					__result = __instance.councilor.location;
				}
				return false;
			}
		}
		//Patch for orbit mission 给干员瞎入轨打个补丁照搬并跳过了原方法
		[HarmonyPatch(typeof(TIMissionCondition_CanLaunchToOrbit), "CanTarget")]
		private class TIMissionCondition_CanLaunchToOrbit_CanTarget_Patch
		{
			private static bool Prefix(TIMissionCondition_CanLaunchToOrbit __instance, ref string __result, TICouncilorState councilor, TIGameState possibleTarget)
			{
				if (((councilor.isHuman && councilor.OnEarth) || councilor.AtABase)&& councilor.ref_spaceBody == possibleTarget.ref_spaceBody)
				{
					__result = "_Pass";
					return false;
				}
				else 
				{
					__result = __instance.GetType().Name;
				}
				return false;
			}
		}
		//Same reason as above 和上面一样懒得用PostFix了照搬并跳过了原方法
		[HarmonyPatch(typeof(TIMissionCondition_AllowedOrbitTarget), "CanTarget")]
		private class TIMissionCondition_AllowedOrbitTarget_CanTarget_Patch
		{
			private static bool Prefix(TIMissionCondition_CanLaunchToOrbit __instance, ref string __result, TICouncilorState councilor, TIGameState possibleTarget)
			{
				if (councilor.ValidDestination(TIUtilities.ObjectToExactLocation(possibleTarget)))
				{
					if (councilor.OnEarth)
					{
						TIOrbitState ref_orbit = possibleTarget.ref_orbit;
						if (ref_orbit != null)
						{
							__result = "_Pass";
							return false;
						}
						TIOrbitState ref_orbit2 = possibleTarget.ref_orbit;
						bool? flag;
						if (ref_orbit2 == null)
						{
							flag = null;
						}
						else
						{
							TINaturalSpaceObjectState barycenter = ref_orbit2.barycenter.barycenter;
							flag = ((barycenter != null) ? new bool?(barycenter.isEarth) : null);
						}
						bool? flag2 = flag;
						if (flag2.GetValueOrDefault())
						{
							__result = "_Pass";
							return false;
						}
					}
					else if (councilor.AtABase)
					{
						TIOrbitState ref_orbit3 = possibleTarget.ref_orbit;
						TIGameState tigameState = ((ref_orbit3 != null) ? ref_orbit3.barycenter : null);
						TIHabState ref_hab = councilor.location.ref_hab;
						TIGameState tigameState2;
						if (ref_hab == null)
						{
							tigameState2 = null;
						}
						else
						{
							TIHabSiteState ref_habSite = ref_hab.ref_habSite;
							tigameState2 = ((ref_habSite != null) ? ref_habSite.parentBody : null);
						}
						if (tigameState == tigameState2)
						{
							TIOrbitState ref_orbit4 = possibleTarget.ref_orbit;
							if (ref_orbit4 != null && ref_orbit4.interfaceOrbit)
							{
								__result = "_Pass";
								return false;
							}
						}
					}
				}
				__result = __instance.GetType().Name;
				return false;
			}
		}
		//Patch for alien targets 下面两个给外星人瞎送陆军打了个补丁
		[HarmonyPatch(typeof(AlienCrashdownOperation), "GetPossibleTargets")]
		private class AlienCrashdownOperation_GetPossibleTargets_Patch
		{
			private static bool Prefix(ref List<TIGameState> __result, TIGameState actorState)
			{
				List<TIGameState> list = new List<TIGameState>();
				List<TIFactionIdeologyTemplate> list2 = (from x in GameStateManager.AllFactions()
														 where x.ideology.proAlien
														 select x into y
														 select y.ideology).ToList<TIFactionIdeologyTemplate>();
				foreach (TIRegionState tiregionState in GameStateManager.AllRegions())
				{
					TIFactionState executiveFaction = tiregionState.nation.executiveFaction;
					if ((!tiregionState.antiSpaceDefenses || (!(executiveFaction == null) && list2.Contains(executiveFaction.ideology)))&&actorState.ref_spaceBody==tiregionState.ref_spaceBody)
					{
						list.Add(tiregionState);
					}
				}
				__result  = list;
				return false;
			}
		}
		[HarmonyPatch(typeof(AlienLandArmyOperation), "GetPossibleTargets")]
		private class AlienLandArmyOperation_GetPossibleTargets_Patch
		{
			private static bool Prefix(ref List<TIGameState> __result, TIGameState actorState)
			{
				List<TIGameState> list = new List<TIGameState>();
				foreach (TIRegionState tiregionState in GameStateManager.AllRegions())
				{
					if ((tiregionState.nation.alienNation || !tiregionState.antiSpaceDefenses || (tiregionState.nation.executiveFaction != null && tiregionState.nation.allies.Contains(GameStateManager.AlienNation()) && tiregionState.nation.executiveFaction.IsAlienProxy))&& actorState.ref_spaceBody == tiregionState.ref_spaceBody)
					{
						list.Add(tiregionState);
					}
				}
				__result = list;
				return false;
			}
		}
		[HarmonyPatch(typeof(TIHabModuleState), "ModulePower")]
		private class TIHabModuleState_ModulePower_Patch
		{
			private static void Postfix(TIHabModuleState __instance, ref int __result)
			{

				if (__instance.templateName == SIConfig.powerReceiver)
				{
					List<TIHabModuleState> transmitterInRange = new List<TIHabModuleState>();
					float transmissionFactor = 0;
					foreach (TIHabModuleState moduleState in __instance.ref_faction.habModules)
					{
						if (__instance.ref_faction.habModules!= null && moduleState.templateName== SIConfig.powerTransmitter && Vector3d.Distance(moduleState.ref_spaceBody.GetGlobalPosition(), __instance.ref_spaceBody.GetGlobalPosition()) < SIConfig.powerTransmittingRange && moduleState.powered)
						{
							transmitterInRange.Add(moduleState);
						}
					}

					foreach (TIHabModuleState moduleState in transmitterInRange) 
					{
						int transmissionTarget = 0;
						foreach (TIHabModuleState moduleState1 in __instance.ref_hab.ref_faction.habModules)
						{
							if (moduleState1.templateName == SIConfig.powerReceiver && Vector3d.Distance(moduleState.ref_spaceBody.GetGlobalPosition(), moduleState1.ref_spaceBody.GetGlobalPosition()) < SIConfig.powerTransmittingRange && moduleState1.powered)
							{
								transmissionTarget++;
							}
						}
						transmissionFactor += 1 / transmissionTarget;
					}
					__result = (int)(transmissionFactor * __instance.moduleTemplate.power)+1;
				}
			}
		}
		//Patch if needed
		/*[HarmonyPatch(typeof(TIHabModuleTemplate), "ProspectivePower", new Type[] { typeof(TISpaceBodyState), typeof(TIFactionState)})]
		private class TIHabModuleTemplate_ProspectivePower_Patch0
		{
			private static void Postfix(TIHabModuleTemplate __instance, TIFactionState faction, TISpaceBodyState spaceBody,ref int __result)
			{
				if (__instance.dataName == SIConfig.powerReceiver)
				{
					float range = 1.8e11f;
					TISpaceBodyState[] spaceBodyStates = GameStateManager.AllSpaceBodies();
					List<TISpaceBodyState> spaceBodyInRange = new List<TISpaceBodyState>();
					int powerModules = 0;
					foreach (TISpaceBodyState spaceBodyState in spaceBodyStates)
					{
						if (Vector3d.Distance(spaceBodyState.ref_spaceBody.GetGlobalPosition(), spaceBody.GetGlobalPosition()) < range)
						{
							foreach (TIHabState habState in spaceBodyState.habs)
							{
								foreach (TIHabModuleState moduleState in habState.AllModules())
								{
									if (moduleState.moduleTemplate.dataName == SIConfig.powerTransmitter && moduleState.ref_faction == faction)
									{
										powerModules++;
									}
								}
							}
						}
					}
					__result = powerModules * __instance.power;
				}
			}
		}
		[HarmonyPatch(typeof(TIHabModuleTemplate), "ProspectivePower", new Type[] { typeof(TIHabSiteState), typeof(TIFactionState)})]
		private class TIHabModuleTemplate_ProspectivePower_Patch1
		{
			private static void Postfix(TIHabModuleTemplate __instance, TIFactionState faction, TIHabSiteState site, ref int __result)
			{
				if (__instance.dataName == SIConfig.powerReceiver)
				{
					TISpaceBodyState[] spaceBodyStates = GameStateManager.AllSpaceBodies();
					List<TISpaceBodyState> spaceBodyInRange = new List<TISpaceBodyState>();
					int powerModules = 0;
					foreach (TISpaceBodyState spaceBody in spaceBodyStates)
					{
						if (Vector3d.Distance(spaceBody.ref_spaceBody.GetGlobalPosition(), site.ref_spaceBody.GetGlobalPosition()) < SIConfig.powerTransmittingRange)
						{
							foreach (TIHabState habState in spaceBody.habs)
							{
								foreach (TIHabModuleState moduleState in habState.AllModules())
								{
									if (moduleState.moduleTemplate.dataName == SIConfig.powerTransmitter && moduleState.ref_faction == faction && moduleState.powered)
									{
										powerModules++;
									}
								}
							}
						}
					}
					__result = powerModules * __instance.power;
				}
			}
		}*/
		[HarmonyPatch(typeof(TIHabModuleTemplate), "ProspectivePower", new Type[] { typeof(TIHabState) })]
		private class TIHabModuleTemplate_ProspectivePower_Patch3
		{
			private static void Postfix(TIHabModuleTemplate __instance,ref int __result, TIHabState hab)
			{
				if (__instance.dataName == SIConfig.powerReceiver)
				{
					List<TIHabModuleState> transmitterInRange = new List<TIHabModuleState>();
					float transmissionFactor = 0;
					foreach (TIHabModuleState moduleState in hab.ref_faction.habModules)
					{
						if (hab.ref_faction.habModules != null && moduleState.templateName == SIConfig.powerTransmitter && Vector3d.Distance(moduleState.ref_spaceBody.GetGlobalPosition(), hab.ref_spaceBody.GetGlobalPosition()) < SIConfig.powerTransmittingRange && moduleState.powered)
						{
							transmitterInRange.Add(moduleState);
						}
					}

					foreach (TIHabModuleState moduleState in transmitterInRange)
					{
						int transmissionTarget = 0;
						foreach (TIHabModuleState moduleState1 in hab.ref_faction.habModules)
						{
							if (moduleState1.templateName == SIConfig.powerReceiver && Vector3d.Distance(moduleState.ref_spaceBody.GetGlobalPosition(), moduleState1.ref_spaceBody.GetGlobalPosition()) < SIConfig.powerTransmittingRange && moduleState1.powered)
							{
								transmissionTarget++;
							}
						}
						transmissionFactor += 1 / transmissionTarget;
					}
					__result = (int)(transmissionFactor * __instance.power) + 1;
				}
			}
		}
		//Enable Interplanetary Nations
		[HarmonyPatch(typeof(RegionController), "ChangeRegionOwner")]
		private class RegionController_ChangeRegionOwner_Patch
		{
			private static bool Prefix(RegionController __instance, RegionControlChanged e, ref NationController ___nationVisualizer)
			{

				TINationState oldNation = e.oldNation;
				TINationState newNation = e.newNation;
				NationController nation2 = __instance.mapVisualizer.GetNation(newNation.templateName);
				NationController nation = __instance.mapVisualizer.GetNation(oldNation.templateName);

				if (oldNation.solarBody == newNation.solarBody)
				{
					nation.regionVisualizers.Remove(__instance);
					nation2.regionVisualizers.Add(__instance);
					___nationVisualizer = nation2;
					__instance.transform.SetParent(nation2.transform);
				}

					__instance.SetBaselineTexture(__instance.region.ref_nation.template.color);

				return false;
			}
		}
		//Color change
		[HarmonyPatch(typeof(RegionController), "MouseOver")]
		private class RegionController_MouseOver_Patch
		{
			private static bool Prefix(RegionController __instance, ref bool ___mouseOver)
			{
				if (!___mouseOver)
				{
					___mouseOver = true;
					__instance.SetHighlightTexture(__instance.region.ref_nation.template.color);
					if (GeneralControlsController.UITargetingMode != null)
					{
						if (GeneralControlsController.UITargetingMode.GetPossibleTargets.Contains(__instance.region.nation.ref_gameState) || GeneralControlsController.UITargetingMode.GetPossibleTargets.Contains(__instance.region.nation.ref_gameState.ref_region))
						{
							TIInputManager.SetCursor(TIInputManager.targetCursorValid, true);
							return false;
						}
						TIInputManager.SetCursor(TIInputManager.targetCursor, true);
					}
				}
				return false;
			}
		}
		[HarmonyPatch(typeof(RegionController), "RestoreRegionTexture")]
		private class RegionController_RestoreRegionTexture_Patch
		{
			private static bool Prefix(RegionController __instance, ref bool ___mouseOver)
			{
				___mouseOver = false;
				if (!(GeneralControlsController.UIOtherSelectedState == __instance.region) && !(GeneralControlsController.UIOtherSelectedState == __instance.region.nation))
				{
					TIGameState uiotherSelectedState = GeneralControlsController.UIOtherSelectedState;
					if (uiotherSelectedState == null || !uiotherSelectedState.isRegionState || !(GeneralControlsController.UIOtherSelectedState.ref_nation == __instance.region.nation))
					{
						if (GeneralControlsController.UIPlayerInTargetingMode && (GeneralControlsController.CurrentValidTarget(__instance.region) || GeneralControlsController.CurrentValidTarget(__instance.region.nation)))
						{
							__instance.SetAllowedTargetTexture(__instance.region.ref_nation.template.color);
							return false;
						}
						if (__instance.region.IsOccupied())
						{
							__instance.SetOccupiedTexture(__instance.region.ref_nation.template.color);
							return false;
						}
						__instance.SetBaselineTexture(__instance.region.ref_nation.template.color);
						return false;
					}
				}
				__instance.SetSelectedTexture(__instance.region.ref_nation.template.color);
				return false;
			}
		}
		[HarmonyPatch(typeof(TINationState), "TransferRegionsControlTo")]
		private class TINationState_TransferRegionsControlTo_Patch
		{
			private static void Prefix(ref List<TIRegionState> regions)
			{
				List<TIRegionState> fixedRegions = new List<TIRegionState>();
				foreach (TIRegionState tiregionState in regions)
				{
					if (tiregionState.population==0)
					{
						tiregionState.populationInMillions=1e-6f;	
					}
					fixedRegions.Add(tiregionState);
				}
				regions = fixedRegions;
			}
		}
		[HarmonyPatch(typeof(TINationState), nameof(TINationState.AbsorbNation))]
		public class AbsorbNationMiltechCalculationPatch
		{
			static void Prefix(out float __state, TINationState __instance, TIFactionState actingFaction, TINationState joiningNationState)
			{
				__state = 0f;

					float joiningNationStatePopulationAndArmiesMultiplier;
					float thisNationStatePopulationAndArmiesMultiplier;

						joiningNationStatePopulationAndArmiesMultiplier = joiningNationState.population_Millions + (joiningNationState.numArmies * 0.5f * joiningNationState.population_Millions) + (joiningNationState.numNavies * 0.5f * joiningNationState.population_Millions);
						thisNationStatePopulationAndArmiesMultiplier = __instance.population_Millions + (__instance.numArmies * 0.5f * __instance.population_Millions) + (__instance.numNavies * 0.5f * __instance.population_Millions);

					__state = (float)((joiningNationState.militaryTechLevel * joiningNationStatePopulationAndArmiesMultiplier + __instance.militaryTechLevel * thisNationStatePopulationAndArmiesMultiplier) / (thisNationStatePopulationAndArmiesMultiplier + joiningNationStatePopulationAndArmiesMultiplier));

			}

			static void Postfix(float __state, TINationState __instance)
			{

					var nationState = Traverse.Create(__instance);
					FileLog.Log($"[PopBasedMiltechCalculation] Game set {__instance.militaryTechLevel}, replaced with {__state}");
					nationState.Property("militaryTechLevel").SetValue(__state);
					__instance.SetDataDirty();

			}
		}
	}
	//Place to store methods
	public class SIConfig 
	{
		public static string powerTransmitter = "PlatformCore";
		public static string powerReceiver = "SolarCollector";
		public static float powerTransmittingRange = 1.8e11f;
	}
	public class SIController
	{
		public static double GenericTransferDV_mps(TIGameState origin0, TIGameState destination0)
		{
			GenericSpaceObject origin = new GenericSpaceObject();
			origin.AssignData(origin0);
			GenericSpaceObject destination = new GenericSpaceObject();
			destination.AssignData(destination0);
			TINaturalSpaceObjectState tinaturalSpaceObjectState = origin.FindCommonBarycenter(destination);
			double relevantSemimajorAxis_m = origin.GetRelevantSemimajorAxis_m(tinaturalSpaceObjectState);
			double relevantSemimajorAxis_m2 = destination.GetRelevantSemimajorAxis_m(tinaturalSpaceObjectState);
			double num2;
			double num3;
			if (Mathd.Approximately(relevantSemimajorAxis_m, relevantSemimajorAxis_m2))
			{
				double num = 6.283185307179586 * Mathd.Sqrt(relevantSemimajorAxis_m * relevantSemimajorAxis_m * relevantSemimajorAxis_m / tinaturalSpaceObjectState.mu);
				num2 = num / 100.0;
				num3 = num / 100.0;
			}
			else
			{
				double num4 = relevantSemimajorAxis_m + relevantSemimajorAxis_m2;
				double num5 = Mathd.Sqrt(tinaturalSpaceObjectState.mu / relevantSemimajorAxis_m);
				double num6 = Mathd.Sqrt(2.0 * relevantSemimajorAxis_m2 / num4) - 1.0;
				num2 = num5 * num6;
				double num7 = Mathd.Sqrt(tinaturalSpaceObjectState.mu / relevantSemimajorAxis_m2);
				double num8 = Mathd.Sqrt(2.0 * relevantSemimajorAxis_m / num4);
				double num9 = 1.0 - num8;
				num3 = num7 * num9;
			}
			return Mathd.Abs(num2 + num3);
		}
	}
	public class TIMissionCondition_FireSatelliteLaserBattery : TIMissionCondition
	{
		// Token: 0x06000569 RID: 1385 RVA: 0x00016D0C File Offset: 0x00014F0C
		public override string CanTarget(TICouncilorState councilor, TIGameState possibleTarget)
		{
			if (councilor.InAHab && councilor.ref_hab.ActiveModules().Any((TIHabModuleState x) => x.moduleTemplate.dataName == "PlatformCore"))//Check by dataName
			{
				return "_Pass";
			}
			return base.GetType().Name;
		}
	}
	public class TIMissionCondition_SatelliteLaserBatteryTarget : TIMissionCondition
	{
		// Token: 0x06000569 RID: 1385 RVA: 0x00016D0C File Offset: 0x00014F0C
		public override string CanTarget(TICouncilorState councilor, TIGameState possibleTarget)
		{
			float range = 3e9f;
			if (possibleTarget.isHabState) 
			{
				TIHabState habState = (TIHabState)possibleTarget;
				if (habState != councilor.ref_hab && Vector3d.Distance(councilor.ref_spaceBody.GetGlobalPosition(), habState.ref_spaceBody.GetGlobalPosition()) < range) 
				{
					return "_Pass";
				}
			}
			if (possibleTarget.isSpaceShipState) 
			{
				TISpaceShipState spaceShipState = (TISpaceShipState)possibleTarget;
				if (spaceShipState.ref_spaceBody!=null && Vector3d.Distance(councilor.ref_spaceBody.GetGlobalPosition(), spaceShipState.ref_spaceBody.GetGlobalPosition()) < range) 
				{
					return "_Pass";
				}
			}
			return base.GetType().Name;
		}
	}

	public class TIMissionEffect_SatelliteLaserBatteryHit : TIMissionEffect
	{
		public override bool HasDelayedEffect()
		{
			return true;
		}
		public override string ApplyEffect(TIMissionState mission, TIGameState target, TIMissionOutcome outcome = TIMissionOutcome.Success)
		{
			SIController sIController = new SIController();
			if (outcome == TIMissionOutcome.Success) 
			{
				if (target.isHabState)
				{
					TIHabState tIHabState = (TIHabState)target;
					TIHabModuleState module = tIHabState.AllModules()[UnityEngine.Random.Range(0, tIHabState.AllModules().Count)];
					module.DestroyModule();
				}
				if (target.isSpaceShipState)
				{
					TISpaceShipState tispaceShipState = (TISpaceShipState)target;
					ModuleDataEntry moduleDataEntry0 = tispaceShipState.radiatorModule;
					tispaceShipState.ApplyDamageToPart(moduleDataEntry0, moduleDataEntry0.moduleTemplate.hitPoints);
					ModuleDataEntry moduleDataEntry1 = tispaceShipState.driveModule;
					tispaceShipState.ApplyDamageToPart(moduleDataEntry1, 0.5f * moduleDataEntry1.moduleTemplate.hitPoints);
				}
			}
			return string.Empty;
		}
		public override void ApplyDelayedEffect(TIMissionState mission, TIGameState target, TIMissionOutcome outcome = TIMissionOutcome.Success, string dataName = "")
		{
			if (outcome == TIMissionOutcome.CriticalSuccess)
			{
				TIFactionState faction = mission.councilor.faction;
				if (target.isHabState)
				{
					TIHabState hab = (TIHabState)target;
					hab.DestroyHab(faction, 0.1f);
					hab.ref_councilor.KillCouncilor();
				}
				if (target.isSpaceShipState)
				{
					TISpaceShipState tispaceShipState = (TISpaceShipState)target;
					tispaceShipState.DestroyShip(true,faction);
				}
			}
		}
	}
	public class TIMissionCondition_FireSatelliteMassDrive : TIMissionCondition
	{
		// Token: 0x06000569 RID: 1385 RVA: 0x00016D0C File Offset: 0x00014F0C
		public override string CanTarget(TICouncilorState councilor, TIGameState possibleTarget)
		{
			if (councilor.InAHab && councilor.ref_hab.ActiveModules().Any((TIHabModuleState x) => x.moduleTemplate.dataName == "PlatformCore"))//Check by dataName
			{
				return "_Pass";
			}
			return base.GetType().Name;
		}
	}
	public class TIMissionCondition_SatelliteMassDriveTarget : TIMissionCondition
	{
		// Token: 0x06000569 RID: 1385 RVA: 0x00016D0C File Offset: 0x00014F0C
		public override string CanTarget(TICouncilorState councilor, TIGameState possibleTarget)
		{
			float range = 8e11f;
			if (possibleTarget.isHabState)
			{
				TIHabState habState = (TIHabState)possibleTarget;
				if (habState != councilor.ref_hab && Vector3d.Distance(councilor.ref_spaceBody.GetGlobalPosition(), habState.ref_spaceBody.GetGlobalPosition()) < range)
				{
					return "_Pass";
				}
			}
			if (possibleTarget.isSpaceShipState)
			{
				TISpaceShipState spaceShipState = (TISpaceShipState)possibleTarget;
				if (spaceShipState.ref_spaceBody != null && Vector3d.Distance(councilor.ref_spaceBody.GetGlobalPosition(), spaceShipState.ref_spaceBody.GetGlobalPosition()) < range)
				{
					return "_Pass";
				}
			}
			return base.GetType().Name;
		}
	}
	public class TIMissionEffect_SatelliteMassDriveHit : TIMissionEffect
	{
		public override bool HasDelayedEffect()
		{
			return true;
		}
		public override string ApplyEffect(TIMissionState mission, TIGameState target, TIMissionOutcome outcome = TIMissionOutcome.Success)
		{
			float deltaV_kps = 50f;
			SIController sIController = new SIController();
			if (outcome == TIMissionOutcome.Success)
			{
				if (target.isHabState)
				{
					TIHabState tIHabState = (TIHabState)target;
					TIHabModuleState module = tIHabState.AllModules()[UnityEngine.Random.Range(0, tIHabState.AllModules().Count)];
					module.DestroyModule();
				}
				if (target.isSpaceShipState)
				{
					TISpaceShipState tispaceShipState = (TISpaceShipState)target;
					tispaceShipState.ConsumeDeltaV(deltaV_kps);
				}
			}
			return string.Empty;
		}
		public override void ApplyDelayedEffect(TIMissionState mission, TIGameState target, TIMissionOutcome outcome = TIMissionOutcome.Success, string dataName = "")
		{
			if (outcome == TIMissionOutcome.CriticalSuccess)
			{
				TIFactionState faction = mission.councilor.faction;
				if (target.isHabState)
				{
					TIHabState hab = (TIHabState)target;
					hab.DestroyHab(faction, 0.1f);
					hab.ref_councilor.KillCouncilor();
				}
				if (target.isSpaceShipState)
				{
					TISpaceShipState tispaceShipState = (TISpaceShipState)target;
					tispaceShipState.DestroyShip(true, faction);
				}
			}
		}
	}
	//Space Marine class
	public class SISpaceMarineState : TIArmyState
	{
		public override bool LegalRegion(TIRegionState region)
		{
			return true;
		}

		public override bool CanHeal()
		{
			return this.strength > 0f && this.strength < 1f && !this.InBattle() && base.CurrentOperations().Count == 0;
		}

		public override string GetModelResource()
		{
			return "simodelpack/drone";
		}

		public override Sprite GetTransportIcon()
		{
			return AssetCacheManager.navyTransportIcon_2;
		}

		public override Sprite GetForegroundIcon()
		{
			if (!this.IsAttacking())
			{
				return AssetCacheManager.humanArmy7_def;
			}
			return AssetCacheManager.humanArmy7_att;
		}

		public override bool CanTakeOffensiveAction
		{
			get
			{
				return true;
			}
		}
		public override float battleValue
		{
			get
			{
				return this.techLevel;
			}
		}
		public override float investmentArmyFactor
		{
			get
			{
				return 3f;
			}
		}
        public override string illustration
		{
			get
			{
					return TemplateManager.global.illus_projectCompletePath[TechCategory.MilitaryScience];
			}
		}
		public override string AnimatorResource
		{
			get
			{
				if (!this.IsAttacking())
				{
					return "TechLvl7_army_def_animator";
				}
				return "TechLvl7_army_att_animator";
			}
		}
		public override string FightingSpriteSheet
		{
			get
			{
				if (!this.IsAttacking())
				{
					return "SpriteSheet_TechLvl7_army_def";
				}
				return "SpriteSheet_TechLvl7_army_att";
			}
		}
		public override string MovingSpriteSheet
		{
			get
			{
				if (!this.IsAttacking())
				{
					return "SpriteSheet_TechLvl7_army_def2";
				}
				return "SpriteSheet_TechLvl7_army_att2";
			}
		}
		public override string GetIconForegroundResource
		{
			get
			{
				if (!this.IsAttacking())
				{
					return TemplateManager.global.pathArmy7_defending;
				}
				return TemplateManager.global.pathArmy7_attacking;
			}
		}
		public override TIControlPoint ref_controlPoint
		{
			get
			{
				return null;
			}
		}
		public override float dailyHealRate
		{
			get
			{
				return 0.01f;
			}
		}
	}
}
