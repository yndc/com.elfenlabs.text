#include <vector>
#include <cstdint>
#include <memory> // For std::vector with custom allocator if needed

// Include Unity Headers (usually found in Editor installation path)
// Adjust path as needed
#include "IUnityInterface.h"
#include "IUnityGraphics.h"

// Include Graphics API Headers (conditionally based on build target)
// Define SUPPORT_D3D11, SUPPORT_METAL etc. in your build system
#if SUPPORT_D3D11
#include <d3d11.h>
#include <UnityGraphicsD3D11.h> // For IUnityGraphicsD3D11 interface
#endif
#if SUPPORT_METAL
#import <Metal/Metal.h>
// Forward declare or include UnityGraphicsMetal.h
struct IUnityGraphicsMetal;
#endif

// --- Data Structures (Must match C# side layout) ---

// Example: Info for one glyph update, passed from C# via event ID pointer
// Ensure this struct is blittable and matches C# layout
struct GlyphGpuUpdateData
{
    int32_t dest_x;
    int32_t dest_y;
    int32_t width;
    int32_t height;
    const unsigned char *pixel_data_ptr; // Pointer to CPU pixel data (e.g., from NativeArray)
    int32_t row_pitch;                   // Bytes per row in pixel_data_ptr
};

// Example: Command data structure holding info for a batch
// Ensure this struct is blittable and matches C# layout
struct AtlasUpdateCommand
{
    void *target_texture_handle; // Native texture pointer (e.g., ID3D11Texture2D*, id<MTLTexture>)
    GlyphGpuUpdateData *glyphs;  // Pointer to an array of GlyphGpuUpdateData in C# memory
    int glyphs_count;
};

static IUnityInterfaces *s_UnityInterfaces = nullptr;
static IUnityGraphics *s_Graphics = nullptr;
static UnityGfxRenderer s_RendererType = kUnityGfxRendererNull;

// Store native device pointers (example for D3D11/Metal)
#if SUPPORT_D3D11
static ID3D11Device *g_D3D11Device = nullptr;
#endif
#if SUPPORT_METAL
static id<MTLDevice> g_MetalDevice = nullptr;
#endif

// --- Plugin Entry Points ---

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
UnityPluginLoad(IUnityInterfaces *unityInterfaces)
{
    s_UnityInterfaces = unityInterfaces;
    s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();

    // Register device event callback
    if (s_Graphics)
    {
        s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
        // Trigger initial event in case device is already loaded
        OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
    }
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
UnityPluginUnload()
{
    if (s_Graphics)
    {
        s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
    }
    s_Graphics = nullptr;
    s_UnityInterfaces = nullptr;
}

// --- Graphics Device Event Handling ---

// Callback for graphics device events (initialization, shutdown)
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
    switch (eventType)
    {
    case kUnityGfxDeviceEventInitialize:
    {
        s_RendererType = s_Graphics->GetRenderer();
// Get and store native device pointers based on s_RendererType
#if SUPPORT_D3D11
        if (s_RendererType == kUnityGfxRendererD3D11)
        {
            IUnityGraphicsD3D11 *d3d = s_UnityInterfaces->Get<IUnityGraphicsD3D11>();
            g_D3D11Device = d3d ? d3d->GetDevice() : nullptr;
        }
#endif
#if SUPPORT_METAL
        if (s_RendererType == kUnityGfxRendererMetal)
        {
            IUnityGraphicsMetal *metal = s_UnityInterfaces->Get<IUnityGraphicsMetal>();
            // Metal device is often obtained differently, maybe stored or retrieved via API
            // g_MetalDevice = metal ? metal->MetalDevice() : nullptr; // Example placeholder
            // Consult Unity Native Metal examples
        }
#endif
        // ... handle other APIs ...
        break;
    }
    case kUnityGfxDeviceEventShutdown:
    {
        s_RendererType = kUnityGfxRendererNull;
// Release stored device pointers
#if SUPPORT_D3D11
        g_D3D11Device = nullptr;
#endif
#if SUPPORT_METAL
        g_MetalDevice = nullptr; // Use appropriate ARC release if needed (__bridge transfer?)
#endif
        // ...
        break;
    }
    // Other events like BeforeReset, AfterReset can be handled if needed
    case kUnityGfxDeviceEventBeforeReset:
    {
        break;
    }
    case kUnityGfxDeviceEventAfterReset:
    {
        break;
    }
    }
}

// --- Render Thread Callback ---

// This function will be called on the render thread when GL.IssuePluginEvent is used.
static void UNITY_INTERFACE_API RenderThreadUpdateAtlas(int eventId) 
{
    // eventId is often used to pass a pointer to command data, cast it back
    // IMPORTANT: Ensure the data pointed to is still valid! (Use persistent allocations in C#
    // and manage lifetime carefully, potentially using GCHandle.ToIntPtr/FromIntPtr)
    AtlasUpdateCommand *command = reinterpret_cast<AtlasUpdateCommand *>(static_cast<intptr_t>(eventId));

    if (!command || !s_Graphics || s_RendererType == kUnityGfxRendererNull)
    {
        // Log Error: Invalid command data or graphics not initialized
        return;
    }

    // --- Platform-Specific Texture Update ---
    // This requires detailed implementation for each supported API.

#if SUPPORT_D3D11
    if (s_RendererType == kUnityGfxRendererD3D11)
    {
        if (!g_D3D11Device)
            return;
        ID3D11DeviceContext *context = nullptr;
        g_D3D11Device->GetImmediateContext(&context);
        if (context)
        {
            ID3D11Resource *targetTextureResource = static_cast<ID3D11Resource *>(command->target_texture_handle);
            if (targetTextureResource)
            {
                for (int i = 0; i < command->glyphs_count; ++i)
                {
                    const auto &update = command->updates[i];
                    // Example using UpdateSubresource (requires texture created appropriately)
                    D3D11_BOX destBox;
                    destBox.left = update.dest_x;
                    destBox.top = update.dest_y; // D3D uses top-left
                    destBox.front = 0;
                    destBox.right = update.dest_x + update.width;
                    destBox.bottom = update.dest_y + update.height;
                    destBox.back = 1;

                    // Calculate subresource index for Texture2DArray slice
                    // Assumes MipLevel 0. Adjust if using mipmaps.
                    UINT subresource = D3D11CalcSubresource(0, command->targetSliceIndex, 1);

                    context->UpdateSubresource(
                        targetTextureResource,
                        subresource,
                        &destBox,              // Destination box within the subresource
                        update.pixel_data_ptr, // Pointer to source CPU data
                        update.row_pitch,      // Source row pitch
                        0                      // Source depth pitch (unused for 2D)
                    );
                    // Alternatively use Map/Unmap on staging or dynamic texture for potentially better performance on some hardware
                }
            }
            context->Release(); // Release the context obtained with GetImmediateContext
        }
    }
#endif // SUPPORT_D3D11

#if SUPPORT_METAL
    if (s_RendererType == kUnityGfxRendererMetal)
    {
        // Requires access to the current command buffer/encoder, often passed via data or obtained via Metal API
        // This is highly simplified - consult Unity Native Metal examples
        /*
        if (!g_MetalDevice) return;
        id<MTLTexture> targetTexture = (__bridge id<MTLTexture>)command->target_texture_handle;
        id<MTLCommandBuffer> commandBuffer = ...; // Obtain current command buffer
        id<MTLBlitCommandEncoder> blitEncoder = [commandBuffer blitCommandEncoder];

        if(targetTexture && blitEncoder) {
            for (int i = 0; i < command->glyphs_count; ++i) {
                const auto& update = command->updates[i];
                MTLRegion region = MTLRegionMake2D(update.dest_x, update.dest_y, update.width, update.height); // Metal uses top-left

                [blitEncoder replaceRegion:region
                               mipmapLevel:0
                                     slice:command->targetSliceIndex
                                 withBytes:update.pixel_data_ptr // Pointer to source CPU data
                               bytesPerRow:update.row_pitch
                             bytesPerImage:0]; // 0 for 2D textures
            }
            [blitEncoder endEncoding];
            // Command buffer commit usually happens later in Unity's frame
        }
        */
    }
#endif // SUPPORT_METAL

    // ... Implementations for Vulkan, OpenGL etc. ...

    // --- IMPORTANT: Data Lifetime ---
    // The 'command' pointer and the 'pixel_data_ptr' buffers it points to
    // MUST remain valid until the GPU finishes processing these commands.
    // C# side needs a robust mechanism (fences, callbacks, waiting frames)
    // to know when it's safe to Dispose the persistent NativeArrays/command struct.
}

// --- Exported Function Pointer Getter ---

// C# calls this once to get the function pointer for the render thread callback.
extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
GetRenderEventFunc()
{
    return RenderThreadUpdateAtlas;
}
