using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer { name = bufferName};

    // Unlit shader only
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    ScriptableRenderContext context;
    Camera camera;

    CullingResults cullingResults;

    public void Render(ScriptableRenderContext context, Camera camera){
        this.context = context;
        this.camera = camera;

        PrepareForSceneWindow();
        if(!Cull()) return;

        // Setup
        Setup();

        // Draws to a buffer
        DrawVisibleGeometry();
        DrawUnsupportedShaders();
        DrawGizmos();

        // Actually draws to the screen
        Submit();
    }

    void Setup(){
        context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(clearDepth:true, clearColor:true, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(){
        var sortingSettings = new SortingSettings(camera){
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
        );
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    void Submit(){
        buffer.EndSample(bufferName);
        ExecuteBuffer();
        context.Submit();
    }
    
    void ExecuteBuffer(){
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull(){
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)){
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

}
