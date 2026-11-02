using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Building;
using SubTerra.Gameplay.Building;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class Issue269BuildingReadabilityTests
    {
        private UiTestEnvironment environment;
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            environment?.Dispose();environment=null;
            var empty=SceneManager.CreateScene("BuildingReadabilityCleanup");SceneManager.SetActiveScene(empty);
            for(int i=SceneManager.sceneCount-1;i>=0;i--)
                if(SceneManager.GetSceneAt(i)!=empty)yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
        }
        private static Transform Named(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        [UnityTest]
        public IEnumerator Bootstrap_Mine_BuildingFlowAndScrollableDetailsAtThreeResolutions()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-subterra-save-root");
            Assert.That(at,Is.GreaterThanOrEqualTo(0));
            Assert.That(Path.GetFullPath(args[at+1]).StartsWith(Path.GetFullPath("Temp")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),Is.True);
            bool before=Array.IndexOf(args,"-subterra-ui-before")>=0;string stage=before?"before":"after";
            environment=new UiTestEnvironment();GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            yield return UiTestWait.Until(()=>SceneManager.GetActiveScene().name=="MainMenu","main menu",60f);
            var runtime=SaveRuntimeController.Instance;Assert.That(runtime.StartNewGame(2,true),Is.True);
            yield return UiTestWait.Until(()=>SceneManager.GetActiveScene().name=="SurfaceBase"&&runtime.IsUiReady,"surface",60f);
            GameBootstrapper.Instance.State.SetDemoProgress(DemoObjectiveIds.MineCopper,1,false);
            GameBootstrapper.Instance.State.SetQuestRewardSettlement(string.Empty,1);
            Assert.That(runtime.TryStartExploration(out var reason),Is.True,reason);
            yield return UiTestWait.Until(()=>SceneManager.GetActiveScene().name=="Mine_Demo_Integration"&&runtime.IsUiReady,"mine",60f);
            yield return UiTestEnvironment.DismissBriefing();environment.BindInput();
            yield return environment.Resolution.Set(1920,1080);
            yield return UiTestWait.Press(environment.Keyboard,Key.B);
            var view=Object.FindAnyObjectByType<BuildingMenuView>();Assert.That(view,Is.Not.Null);
            var panel=view.transform.Find("PanelRoot").gameObject;
            var binder=view.GetComponent<BuildingMenuBinder>();
            yield return new WaitForSecondsRealtime(.3f);Capture(panel,stage+"-idle");
            var support=Named(panel.transform,"Select_"+DataIds.Buildings.SupportBasic).GetComponent<Button>();
            yield return UiTestWait.Click(environment.Mouse,support);
            Assert.That(binder.Presenter.SelectedBuildingId,Is.EqualTo(DataIds.Buildings.SupportBasic));
            Assert.That(Object.FindAnyObjectByType<BuildingPlacementSystem>().Selection,Is.Not.Null,"Preview started");
            var list=panel.transform.Find("FacilityScroll")?.GetComponent<ScrollRect>();
            if(list!=null){list.verticalNormalizedPosition=0f;yield return null;}
            var core=Named(panel.transform,"Select_"+DataIds.Buildings.OutpostCoreBasic).GetComponent<Button>();
            yield return UiTestWait.Click(environment.Mouse,core);
            Assert.That(binder.Presenter.SelectedBuildingId,Is.EqualTo(DataIds.Buildings.OutpostCoreBasic));
            Assert.That(Named(panel.transform,"AvailabilityText").GetComponent<TMP_Text>().text,Does.Contain("자원 부족"));
            foreach(var size in new[]{new Vector2Int(1366,768),new Vector2Int(1920,1080),new Vector2Int(2560,1440)})
            {
                yield return environment.Resolution.Set(size.x,size.y);yield return null;
                Capture(panel,stage+"-poor-"+size.x);
            }
            yield return environment.Resolution.Set(1920,1080);
            var inventory=runtime.InventoryService;
            Assert.That(inventory.TryAddMineralExact(DataIds.Minerals.Copper,6).DidChange,Is.True);
            Assert.That(inventory.TryAddMineralExact(DataIds.Minerals.Iron,6).DidChange,Is.True);
            yield return null;
            Assert.That(Named(panel.transform,"CostRow_0").GetComponentInChildren<BuildingMenuCostRowView>().gameObject.activeSelf,Is.True);
            Capture(panel,stage+"-resources-updated");
            // Render the ready-state read model separately from the live cursor's out-of-range state.
            view.SetAvailability(new BuildingAvailabilityReadModel(BuildingPlacementState.Valid,true,string.Empty));
            Capture(panel,stage+"-ready-state");
            // Stress only the View read model; these values never alter game costs or saves.
            var costs=new[]{new BuildingCostReadModel(DataIds.Minerals.Iron,5,3),new BuildingCostReadModel(DataIds.Minerals.Copper,4,10),new BuildingCostReadModel(DataIds.Minerals.Lithium,3,1)};
            string description=string.Concat(Enumerable.Repeat("시설의 용도와 긴 운영 설명을 충분한 공간에서 읽을 수 있어야 합니다. ",8));
            view.SetSelection(new BuildingMenuItemReadModel("qa.long-building","긴 시설명 표시 검사용 설비",description,null,0,costs));
            view.SetAvailability(new BuildingAvailabilityReadModel(BuildingPlacementState.Invalid,false,"시설을 지지할 지면이 없습니다."));
            view.SetStatusMessage("설치 결과 메시지 전용 줄");
            yield return null;
            Capture(panel,stage+"-long-top");
            var scroll=panel.transform.Find("DetailScroll")?.GetComponent<ScrollRect>();
            if(scroll!=null)
            {
                Vector3 banner=panel.transform.Find("AvailabilityBanner").position;
                Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));
                scroll.verticalNormalizedPosition=0f;yield return null;Capture(panel,stage+"-long-bottom");
                Assert.That(panel.transform.Find("AvailabilityBanner").position,Is.EqualTo(banner));
                Assert.That(Named(panel.transform,"AvailabilityText").GetComponent<TMP_Text>().text,Does.Contain("지면"));
                var points=new Vector3[4];var prior=new Vector3[4];
                var row=Named(panel.transform,"CostRow_0");
                ((RectTransform)row.Find("Name")).GetWorldCorners(points);
                ((RectTransform)row.Find("Amount")).GetWorldCorners(prior);
                Assert.That(prior[0].x,Is.GreaterThan(points[2].x));
                ((RectTransform)row.Find("State")).GetWorldCorners(points);
                Assert.That(points[0].x,Is.GreaterThan(prior[2].x));
            }
            yield return UiTestWait.Press(environment.Keyboard,Key.B);
            Assert.That(binder.Presenter.SelectedBuildingId,Is.Empty);
            yield return UiTestWait.Press(environment.Keyboard,Key.B);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Named(panel.transform,"DetailName").GetComponent<TMP_Text>().text,Is.EqualTo("시설 미선택"));
            yield return UiTestWait.Click(environment.Mouse,view.CloseButton);
            yield return UiTestWait.Until(()=>!panel.activeInHierarchy,"X close",5f);
            Assert.That(scroll,Is.Not.Null,"Long descriptions require an isolated detail scroll area");
        }
        private static void Capture(GameObject panel, string name)
        {
            var canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
            var owner = new GameObject("BuildingCaptureCamera", typeof(Camera));
            var camera = owner.GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.015f, .025f, .035f);
            camera.cullingMask = 1 << panel.layer;
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance;
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                image.Apply();
                string directory = Path.GetFullPath("Builds/Issue269-20261103");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousDistance;
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(image);
                Object.Destroy(owner);
            }
        }

    }
}
