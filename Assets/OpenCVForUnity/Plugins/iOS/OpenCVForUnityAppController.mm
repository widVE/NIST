#import "UnityAppController.h"
#include "Unity/IUnityGraphics.h"

extern "C" void OpenCVForUnity_UnityPluginLoad(IUnityInterfaces *interfaces);
extern "C" void OpenCVForUnity_UnityPluginUnload();

#pragma mark - App controller subclasssing

@interface OpenCVForUnityAppController : UnityAppController
{
}
- (void)shouldAttachRenderDelegate;
@end

@implementation OpenCVForUnityAppController
- (void)shouldAttachRenderDelegate;
{
    UnityRegisterRenderingPluginV5(&OpenCVForUnity_UnityPluginLoad, &OpenCVForUnity_UnityPluginUnload);
}
@end

IMPL_APP_CONTROLLER_SUBCLASS(OpenCVForUnityAppController);
