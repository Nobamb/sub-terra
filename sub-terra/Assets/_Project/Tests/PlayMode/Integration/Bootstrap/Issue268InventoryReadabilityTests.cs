using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class Issue268InventoryReadabilityTests
    {
        private UiTestEnvironment environment;
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            environment?.Dispose(); environment = null;
            var empty = SceneManager.CreateScene("InventoryReadabilityCleanup");
            SceneManager.SetActiveScene(empty);
            for(int i=SceneManager.sceneCount-1;i>=0;i--)
                if(SceneManager.GetSceneAt(i)!=empty) yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
        }

        [UnityTest]
        public IEnumerator Bootstrap_SurfaceAndMine_InventoryColumnsStatesAndInput()
        {
            var args=Environment.GetCommandLineArgs();
            int at=Array.IndexOf(args,"-subterra-save-root");
            Assert.That(at,Is.GreaterThanOrEqualTo(0));
            Assert.That(Path.GetFullPath(args[at+1]).StartsWith(Path.GetFullPath("Temp")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),Is.True);
            bool before=Array.IndexOf(args,"-subterra-ui-before")>=0;
            string stage=before?"before":"after";
            environment=new UiTestEnvironment();
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            yield return UiTestWait.Until(()=>SceneManager.GetActiveScene().name=="MainMenu","main menu",60f);
            var runtime=SaveRuntimeController.Instance;
            Assert.That(runtime.StartNewGame(2,true),Is.True);
            yield return UiTestWait.Until(()=>SceneManager.GetActiveScene().name=="SurfaceBase"&&runtime.IsUiReady,"surface ready",60f);
            GameBootstrapper.Instance.State.SetDemoProgress(DemoObjectiveIds.MineCopper,1,false);
            GameBootstrapper.Instance.State.SetQuestRewardSettlement(string.Empty,1);
            bool columns=true;
            for(int context=0;context<2;context++)
            {
                if(context==1)
                {
                    Assert.That(runtime.TryStartExploration(out var reason),Is.True,reason);
                    yield return UiTestWait.Until(()=>SceneManager.GetActiveScene().name=="Mine_Demo_Integration"&&runtime.IsUiReady,"mine ready",60f);
                    yield return UiTestEnvironment.DismissBriefing();
                }
                environment.BindInput();
                var inventory=runtime.InventoryService;
                foreach(var stack in inventory.GetSnapshot().Stacks) inventory.TryReduceMineral(stack.MineralId,stack.Quantity);
                yield return UiTestWait.Press(environment.Keyboard,Key.I);
                var view=Object.FindAnyObjectByType<InventoryPanelView>(FindObjectsInactive.Include);
                Assert.That(view.PanelRoot.activeInHierarchy,Is.True);
                yield return new WaitForSecondsRealtime(.3f);
                Capture(view.PanelRoot,stage+"-empty-"+context);
                if(!before) Assert.That(view.PanelRoot.transform.Find("EmptyHint").GetComponent<TMP_Text>().text,Is.EqualTo("보유 중인 자원이 없습니다"));
                Assert.That(inventory.TryAddMineralExact(DataIds.Minerals.Copper,4).DidChange,Is.True);
                Assert.That(inventory.TryAddMineralExact(DataIds.Minerals.Iron,3).DidChange,Is.True);
                Assert.That(inventory.TryAddMineralExact(DataIds.RareItems.EngineFuel,1).DidChange,Is.True);
                foreach(var size in new[]{new Vector2Int(1366,768),new Vector2Int(1920,1080),new Vector2Int(2560,1440)})
                {
                    yield return environment.Resolution.Set(size.x,size.y);
                    yield return null;
                    var panel=view.PanelRoot.transform;
                    Assert.That(panel.Find("ValueCard/UnsettledAmountText").GetComponent<TMP_Text>().text,
                        Is.EqualTo(InventoryPanelDisplayFormatter.UnsettledAmount(inventory.UnsettledValue)));
                    foreach(var row in view.GetComponentsInChildren<InventoryStackRowView>(true))
                    {
                        var qty=(RectTransform)row.transform.Find("Quantity");
                        columns &= Mathf.Abs(qty.sizeDelta.x-104f)<.1f;
                    }
                    Capture(view.PanelRoot,stage+"-"+context+"-"+size.x);
                    var help=panel.Find("WeightHelpIcon").GetComponent<InventoryWeightTooltip>();
                    help.OnPointerEnter(null);
                    yield return null;
                    Capture(view.PanelRoot,stage+"-help-"+context+"-"+size.x);
                    if(!before)
                    {
                        var tooltip=(RectTransform)help.TooltipRoot.transform;
                        var points=new Vector3[4]; tooltip.GetWorldCorners(points);
                        Assert.That(points[0].y,Is.GreaterThanOrEqualTo(0f));
                        Assert.That(points[2].x,Is.LessThanOrEqualTo(Screen.width));
                        var cargo=(RectTransform)panel.Find("CargoCard");
                        cargo.GetWorldCorners(points); float cardBottom=points[0].y;
                        tooltip.GetWorldCorners(points);
                        Assert.That(points[2].y,Is.LessThan(cardBottom),"Tooltip covers cargo card");
                    }
                    help.OnPointerExit(null);
                }
                // Display-only stress values never mutate the QA game inventory.
                var scroll=view.PanelRoot.transform.Find("StackScroll").GetComponent<ScrollRect>();
                var cardPosition=view.PanelRoot.transform.Find("CargoCard").position;
                scroll.verticalNormalizedPosition=0f;
                yield return null;
                Assert.That(view.PanelRoot.transform.Find("CargoCard").position,Is.EqualTo(cardPosition));
                Capture(view.PanelRoot,stage+"-scroll-"+context);
                scroll.verticalNormalizedPosition=1f;
                var icon=view.GetComponentInChildren<InventoryStackRowView>(true).transform.Find("Icon").GetComponent<Image>().sprite;
                view.SetStacks(new[]{new InventoryStackReadModel(DataIds.Minerals.Copper,"정제된 고순도 구리 광석 덩어리",icon,1234567,1.5f)});
                view.SetUnsettledAmount(1234567f);
                view.SetCargoLoad(85,100);
                yield return null;
                Capture(view.PanelRoot,stage+"-long-"+context);
                if(!before)
                {
                    foreach(var text in view.PanelRoot.GetComponentsInChildren<TMP_Text>())
                    { text.ForceMeshUpdate(); Assert.That(text.isTextTruncated,Is.False,text.name+" "+text.text); }
                    foreach(var row in view.GetComponentsInChildren<InventoryStackRowView>(true))
                    {
                        var name=row.transform.Find("Name").GetComponent<TMP_Text>();
                        name.ForceMeshUpdate();
                        Assert.That(name.maxVisibleLines,Is.EqualTo(2));
                        Assert.That(name.renderedHeight,Is.LessThanOrEqualTo(name.rectTransform.rect.height+1f),"Name overflows its two-line area");
                    }
                    Assert.That(view.PanelRoot.transform.Find("CargoCard/CargoStateText").GetComponent<TMP_Text>().text,Does.Contain("적재 한계 임박"));
                    view.SetCargoLoad(100,100);
                    Capture(view.PanelRoot,stage+"-full-"+context);
                }
                // Use the same input resolution for both contexts after the layout sweep.
                yield return environment.Resolution.Set(1920,1080);
                yield return UiTestWait.Click(environment.Mouse,view.CloseButton);
                yield return UiTestWait.Until(()=>!view.PanelRoot.activeInHierarchy,"inventory closed",5f);
                Assert.That(inventory.TryReduceMineral(DataIds.Minerals.Copper,1).DidChange,Is.True);
                yield return UiTestWait.Press(environment.Keyboard,Key.I);
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(view.PanelRoot.activeInHierarchy,Is.True);
                Assert.That(view.PanelRoot.transform.Find("ValueCard/UnsettledAmountText").GetComponent<TMP_Text>().text,
                    Is.EqualTo(InventoryPanelDisplayFormatter.UnsettledAmount(inventory.UnsettledValue)));
                yield return UiTestWait.Press(environment.Keyboard,Key.I);
                yield return UiTestWait.Until(()=>!view.PanelRoot.activeInHierarchy,"I close",5f);
            }
            Assert.That(columns,Is.True,"Quantity columns require width 104");
        }
        private static void Capture(GameObject panel, string name)
        {
            var canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
            var owner = new GameObject("InventoryCaptureCamera", typeof(Camera));
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
                string directory = Path.GetFullPath("Builds/Issue268-20261103");
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
