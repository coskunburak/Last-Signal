#if UNITY_EDITOR
using System.Collections;
using System.IO;
using LastSignal.Noise;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LastSignal.Tests
{
    public sealed class R04IntegrationPlayTests
    {
        const string Evidence = "Docs/Implementation/PreS010-Recovery/Evidence/R04/";
        SessionFlow flow;
        string evidencePath;
        IEnumerator Setup()
        {
            evidencePath = Evidence + System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + "/";
            Directory.CreateDirectory(evidencePath);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/LastSignal/Scenes/Validation/ZombieAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); flow.Resume();
        }

        [UnityTest] public IEnumerator RouteA_QuietTraversal()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            yield return R04AcceptanceRoute.RouteA_QuietTraversal(flow, actor, line => File.AppendAllText(evidencePath + "route-a.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteB_SprintConsequence()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            yield return R04AcceptanceRoute.RouteB_SprintConsequence(flow, actor, line => File.AppendAllText(evidencePath + "route-b.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteC_SilentReposition()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            yield return R04AcceptanceRoute.RouteC_SilentReposition(flow, actor, line => File.AppendAllText(evidencePath + "route-c.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteD_CrowbarTradeoff()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            yield return R04AcceptanceRoute.RouteD_CrowbarTradeoff(flow, actor, line => File.AppendAllText(evidencePath + "route-d.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteE_RifleEscalation()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            yield return R04AcceptanceRoute.RouteE_RifleEscalation(flow, actor, line => File.AppendAllText(evidencePath + "route-e.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteF_WallGunshot()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            yield return R04AcceptanceRoute.RouteF_WallGunshot(flow, actor, line => File.AppendAllText(evidencePath + "route-f.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteG_InvestigateSearchEscape()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            yield return R04AcceptanceRoute.RouteG_InvestigateSearchEscape(flow, actor, line => File.AppendAllText(evidencePath + "route-g.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteH_VisionTakeover()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            yield return R04AcceptanceRoute.RouteH_VisionTakeover(flow, actor, line => File.AppendAllText(evidencePath + "route-h.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteI_BreakLOS()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            // Need to get into Chasing first
            flow.Player.GetComponent<FirstPersonMotor>().enabled = false;
            actor.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(new Vector3(-5, 0, -4)); actor.transform.rotation = Quaternion.identity;
            R04AcceptanceRoute.Place(flow.Player, new Vector3(-5, 0, 0));
            yield return R04AcceptanceRoute.State(actor, ZombieState.Chasing, 3);
            yield return R04AcceptanceRoute.RouteI_BreakLOS(flow, actor, line => File.AppendAllText(evidencePath + "route-i.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteJ_ChaseLossNewSound()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            yield return R04AcceptanceRoute.RouteJ_ChaseLossNewSound(flow, actor, line => File.AppendAllText(evidencePath + "route-j.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteK_MultiZombieGunshot()
        {
            yield return Setup();
            yield return R04AcceptanceRoute.RouteK_MultiZombieGunshot(flow, line => File.AppendAllText(evidencePath + "route-k.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteL_NoTelepathy()
        {
            yield return Setup();
            yield return R04AcceptanceRoute.RouteL_NoTelepathy(flow, line => File.AppendAllText(evidencePath + "route-l.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteM_CombatDeath()
        {
            yield return Setup();
            yield return R04AcceptanceRoute.RouteM_CombatDeath(flow, line => File.AppendAllText(evidencePath + "route-m.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteN_Pause()
        {
            yield return Setup();
            var actor = flow.GetComponent<ZombieEncounter>().Actor;
            yield return R04AcceptanceRoute.RouteN_Pause(flow, actor, line => File.AppendAllText(evidencePath + "route-n.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator RouteO_SessionReset()
        {
            yield return Setup();
            yield return R04AcceptanceRoute.RouteO_SessionReset(flow, line => File.AppendAllText(evidencePath + "route-o.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator PerformanceThirtyAgentIntegrated()
        {
            yield return Setup();
            yield return R04AcceptanceRoute.MeasurePerformance(flow, 30, line => File.AppendAllText(evidencePath + "performance.txt", line + "\n"));
        }

        [UnityTest] public IEnumerator SignatureIntegratedScenario()
        {
            yield return Setup();
            File.WriteAllText(evidencePath + "signature-scenario.txt", "");
            yield return R04AcceptanceRoute.SignatureScenario(flow, line => File.AppendAllText(evidencePath + "signature-scenario.txt", line + "\n"));
        }

        [UnityTearDown] public IEnumerator Cleanup() { if (flow) flow.ReturnToMenu(); Time.timeScale = 1; yield return null; }
    }
}
#endif
