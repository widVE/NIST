#include "pch.h"
#include "HL2ResearchMode.h"
#include "HL2ResearchMode.g.cpp"
#include <codecvt>
#include <fstream>

extern "C"
HMODULE LoadLibraryA(
    LPCSTR lpLibFileName
);

static ResearchModeSensorConsent camAccessCheck;
static HANDLE camConsentGiven;

using namespace DirectX;
using namespace winrt::Windows::Perception;
using namespace winrt::Windows::Perception::Spatial;
using namespace winrt::Windows::Perception::Spatial::Preview;

typedef std::chrono::duration<int64_t, std::ratio<1, 10'000'000>> HundredsOfNanoseconds;

namespace winrt::HL2UnityPlugin::implementation
{
    HL2ResearchMode::HL2ResearchMode() 
    {
        // Load Research Mode library
        camConsentGiven = CreateEvent(nullptr, true, false, nullptr);
        HMODULE hrResearchMode = LoadLibraryA("ResearchModeAPI");
        HRESULT hr = S_OK;

        if (hrResearchMode)
        {
            typedef HRESULT(__cdecl* PFN_CREATEPROVIDER) (IResearchModeSensorDevice** ppSensorDevice);
            PFN_CREATEPROVIDER pfnCreate = reinterpret_cast<PFN_CREATEPROVIDER>(GetProcAddress(hrResearchMode, "CreateResearchModeSensorDevice"));
            if (pfnCreate)
            {
                winrt::check_hresult(pfnCreate(&m_pSensorDevice));
            }
            else
            {
                winrt::check_hresult(E_INVALIDARG);
            }
        }

        // get spatial locator of rigNode
        GUID guid;
        IResearchModeSensorDevicePerception* pSensorDevicePerception;
        winrt::check_hresult(m_pSensorDevice->QueryInterface(IID_PPV_ARGS(&pSensorDevicePerception)));
        winrt::check_hresult(pSensorDevicePerception->GetRigNodeId(&guid));
        pSensorDevicePerception->Release();
        m_locator = SpatialGraphInteropPreview::CreateLocatorForNode(guid);
        
        
        size_t sensorCount = 0;

        winrt::check_hresult(m_pSensorDevice->QueryInterface(IID_PPV_ARGS(&m_pSensorDeviceConsent)));
        winrt::check_hresult(m_pSensorDeviceConsent->RequestCamAccessAsync(HL2ResearchMode::CamAccessOnComplete));

        m_pSensorDevice->DisableEyeSelection();

        winrt::check_hresult(m_pSensorDevice->GetSensorCount(&sensorCount));
        m_sensorDescriptors.resize(sensorCount);
        winrt::check_hresult(m_pSensorDevice->GetSensorDescriptors(m_sensorDescriptors.data(), m_sensorDescriptors.size(), &sensorCount));
        
        m_QRMatrix = XMMatrixIdentity();

        /*winrt::Windows::Perception::Spatial::SpatialCoordinateSystem m_worldOrigin = m_locator.CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem();

        OutputDebugString(L"Initializing Video Frame Streamer...\n");
        m_pVideoFrameStreamer = std::make_shared<VideoCameraStreamer>(m_worldOrigin, L"23940");
        if (!m_pVideoFrameStreamer.get())
        {
            throw winrt::hresult(E_POINTER);
        }

        OutputDebugString(L"Initializing Video Frame Processor...\n");
        m_pVideoFrameProcessor = std::make_unique<VideoCameraFrameProcessor>();*/

        // initialize the frame processor with a streamer sink
        winrt::Windows::Foundation::IAsyncAction processOp{ InitializePVCamera() };
        processOp.get();

    }

    winrt::Windows::Foundation::IAsyncAction HL2ResearchMode::InitializePVCamera()
    {
        OutputDebugString(L"Initializing Async PV Camera...\n");
        //co_await m_pVideoFrameProcessor->InitializeAsync(m_pVideoFrameStreamer);
        winrt::Windows::Foundation::Collections::IVectorView<winrt::Windows::Media::Capture::Frames::MediaFrameSourceGroup>
            mediaFrameSourceGroups{ co_await winrt::Windows::Media::Capture::Frames::MediaFrameSourceGroup::FindAllAsync() };

        winrt::Windows::Media::Capture::Frames::MediaFrameSourceGroup selectedSourceGroup = nullptr;
        winrt::Windows::Media::Capture::MediaCaptureVideoProfile profile = nullptr;
        winrt::Windows::Media::Capture::MediaCaptureVideoProfileMediaDescription desc = nullptr;
        std::vector<winrt::Windows::Media::Capture::Frames::MediaFrameSourceInfo> selectedSourceInfos;

        // Find MediaFrameSourceGroup
        for (const winrt::Windows::Media::Capture::Frames::MediaFrameSourceGroup& mediaFrameSourceGroup : mediaFrameSourceGroups)
        {
            auto knownProfiles = winrt::Windows::Media::Capture::MediaCapture::FindKnownVideoProfiles(
                mediaFrameSourceGroup.Id(),
                winrt::Windows::Media::Capture::KnownVideoProfile::VideoConferencing);

            for (const auto& knownProfile : knownProfiles)
            {
                for (auto knownDesc : knownProfile.SupportedRecordMediaDescription())
                {
                    if ((knownDesc.Width() == 760)) // && (std::round(knownDesc.FrameRate()) == 15))
                    {
                        profile = knownProfile;
                        desc = knownDesc;
                        selectedSourceGroup = mediaFrameSourceGroup;
                        break;
                    }
                }
            }
        }

        winrt::check_bool(selectedSourceGroup != nullptr);

        for (auto sourceInfo : selectedSourceGroup.SourceInfos())
        {
            // Workaround since multiple Color sources can be found,
            // and not all of them are necessarily compatible with the selected video profile
            if (sourceInfo.SourceKind() == winrt::Windows::Media::Capture::Frames::MediaFrameSourceKind::Color)
            {
                selectedSourceInfos.push_back(sourceInfo);
            }
        }
        winrt::check_bool(!selectedSourceInfos.empty());

        // Initialize a MediaCapture object
        winrt::Windows::Media::Capture::MediaCaptureInitializationSettings settings;
        settings.VideoProfile(profile);
        settings.RecordMediaDescription(desc);
        settings.VideoDeviceId(selectedSourceGroup.Id());
        settings.StreamingCaptureMode(winrt::Windows::Media::Capture::StreamingCaptureMode::Video);
        settings.MemoryPreference(winrt::Windows::Media::Capture::MediaCaptureMemoryPreference::Cpu);
        settings.SharingMode(winrt::Windows::Media::Capture::MediaCaptureSharingMode::ExclusiveControl);
        settings.SourceGroup(selectedSourceGroup);

        winrt::Windows::Media::Capture::MediaCapture mediaCapture = winrt::Windows::Media::Capture::MediaCapture();
        co_await mediaCapture.InitializeAsync(settings);

        winrt::Windows::Media::Capture::Frames::MediaFrameSource selectedSource = nullptr;
        winrt::Windows::Media::Capture::Frames::MediaFrameFormat preferredFormat = nullptr;

        for (winrt::Windows::Media::Capture::Frames::MediaFrameSourceInfo sourceInfo : selectedSourceInfos)
        {
            auto tmpSource = mediaCapture.FrameSources().Lookup(sourceInfo.Id());
            for (winrt::Windows::Media::Capture::Frames::MediaFrameFormat format : tmpSource.SupportedFormats())
            {
                if (format.VideoFormat().Width() == 760)
                {
                    selectedSource = tmpSource;
                    preferredFormat = format;
                    break;
                }
            }
        }

        winrt::check_bool(preferredFormat != nullptr);

        co_await selectedSource.SetFormatAsync(preferredFormat);
        m_mediaFrameReader = co_await mediaCapture.CreateFrameReaderAsync(selectedSource);
    }

    void HL2ResearchMode::InitializeDepthSensor() 
    {
       
        for (auto sensorDescriptor : m_sensorDescriptors)
        {
            if (sensorDescriptor.sensorType == DEPTH_AHAT)
            {
                winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_depthSensor));
                winrt::check_hresult(m_depthSensor->QueryInterface(IID_PPV_ARGS(&m_pDepthCameraSensor)));
                winrt::check_hresult(m_pDepthCameraSensor->GetCameraExtrinsicsMatrix(&m_depthCameraPose));
                m_depthCameraPoseInvMatrix = XMMatrixInverse(nullptr, XMLoadFloat4x4(&m_depthCameraPose));
                break;
            }
        }
    }

    void HL2ResearchMode::InitializeLongDepthSensor()
    {
        for (auto sensorDescriptor : m_sensorDescriptors)
        {
            if (sensorDescriptor.sensorType == DEPTH_LONG_THROW)
            {
                winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_longDepthSensor));
                winrt::check_hresult(m_longDepthSensor->QueryInterface(IID_PPV_ARGS(&m_pLongDepthCameraSensor)));
                winrt::check_hresult(m_pLongDepthCameraSensor->GetCameraExtrinsicsMatrix(&m_longDepthCameraPose));
                m_longDepthCameraPoseInvMatrix = XMMatrixInverse(nullptr, XMLoadFloat4x4(&m_longDepthCameraPose));
                break;
            }
        }
    }

    void HL2ResearchMode::InitializeSpatialCamerasFront()
    {
        for (auto sensorDescriptor : m_sensorDescriptors)
        {
            if (sensorDescriptor.sensorType == LEFT_FRONT)
            {
                winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_LFSensor));
                winrt::check_hresult(m_LFSensor->QueryInterface(IID_PPV_ARGS(&m_LFCameraSensor)));
                winrt::check_hresult(m_LFCameraSensor->GetCameraExtrinsicsMatrix(&m_LFCameraPose));
                m_LFCameraPoseInvMatrix = XMMatrixInverse(nullptr, XMLoadFloat4x4(&m_LFCameraPose));
            }
            
            if (sensorDescriptor.sensorType == LEFT_LEFT)
            {
                winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_LFSensorSide));
                winrt::check_hresult(m_LFSensorSide->QueryInterface(IID_PPV_ARGS(&m_LFCameraSensorSide)));
                winrt::check_hresult(m_LFCameraSensorSide->GetCameraExtrinsicsMatrix(&m_LFCameraPoseSide));
                m_LFCameraPoseInvMatrixSide = XMMatrixInverse(nullptr, XMLoadFloat4x4(&m_LFCameraPoseSide));
            }

            if (sensorDescriptor.sensorType == RIGHT_FRONT)
            {
                winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_RFSensor));
                winrt::check_hresult(m_RFSensor->QueryInterface(IID_PPV_ARGS(&m_RFCameraSensor)));
                winrt::check_hresult(m_RFCameraSensor->GetCameraExtrinsicsMatrix(&m_RFCameraPose));
                m_RFCameraPoseInvMatrix = XMMatrixInverse(nullptr, XMLoadFloat4x4(&m_RFCameraPose));
            }

            if (sensorDescriptor.sensorType == RIGHT_RIGHT)
            {
                winrt::check_hresult(m_pSensorDevice->GetSensor(sensorDescriptor.sensorType, &m_RFSensorSide));
                winrt::check_hresult(m_RFSensorSide->QueryInterface(IID_PPV_ARGS(&m_RFCameraSensorSide)));
                winrt::check_hresult(m_RFCameraSensorSide->GetCameraExtrinsicsMatrix(&m_RFCameraPoseSide));
                m_RFCameraPoseInvMatrixSide = XMMatrixInverse(nullptr, XMLoadFloat4x4(&m_RFCameraPoseSide));
            }
        }
    }

    void HL2ResearchMode::StartPVCameraLoop()
    {
        StartColorAsync();
    }

    winrt::Windows::Foundation::IAsyncAction HL2ResearchMode::StartColorAsync()
    {
        winrt::Windows::Media::Capture::Frames::MediaFrameReaderStartStatus status = co_await m_mediaFrameReader.StartAsync();
        winrt::check_bool(status == winrt::Windows::Media::Capture::Frames::MediaFrameReaderStartStatus::Success);

		m_PVLoopStarted = true;

        m_OnFrameArrivedRegistration = m_mediaFrameReader.FrameArrived(
            { this, &HL2ResearchMode::OnFrameArrived });
    }

    void HL2ResearchMode::StartDepthSensorLoop() 
    {
        //std::thread th1([this] {this->DepthSensorLoopTest(); });
        if (m_refFrame == nullptr) 
        {
            m_refFrame = m_locator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem();
        }

        m_pDepthUpdateThread = new std::thread(HL2ResearchMode::DepthSensorLoop, this);
    }

    void HL2ResearchMode::OnFrameArrived(
        const winrt::Windows::Media::Capture::Frames::MediaFrameReader& sender,
        const winrt::Windows::Media::Capture::Frames::MediaFrameArrivedEventArgs& args)
    {
        //std::lock_guard<std::mutex> l(mu);
        mu.lock();

        if (winrt::Windows::Media::Capture::Frames::MediaFrameReference frame = sender.TryAcquireLatestFrame())
        {
            m_latestFrame = frame;
        }
        mu.unlock();
    }

    winrt::Windows::Foundation::IAsyncAction CreateLocalFile(const wchar_t* sName, winrt::Windows::Graphics::Imaging::SoftwareBitmap softwareBitmap, bool bitmapOverride=false)
    {
        winrt::Windows::Storage::StorageFolder storageFolder = winrt::Windows::Storage::ApplicationData::Current().LocalFolder();
        winrt::Windows::Storage::StorageFile saveTest = co_await storageFolder.CreateFileAsync(hstring(sName), winrt::Windows::Storage::CreationCollisionOption::ReplaceExisting);

        
        winrt::Windows::Storage::Streams::IRandomAccessStream outputStream = co_await saveTest.OpenAsync(winrt::Windows::Storage::FileAccessMode::ReadWrite);
        
        if (bitmapOverride)
        {
            winrt::Windows::Graphics::Imaging::BitmapEncoder be = co_await winrt::Windows::Graphics::Imaging::BitmapEncoder::CreateAsync(winrt::Windows::Graphics::Imaging::BitmapEncoder::BmpEncoderId(), outputStream);

            be.SetSoftwareBitmap(softwareBitmap);

            co_await be.FlushAsync();
        }
        else
        {
            winrt::Windows::Graphics::Imaging::BitmapEncoder be = co_await winrt::Windows::Graphics::Imaging::BitmapEncoder::CreateAsync(winrt::Windows::Graphics::Imaging::BitmapEncoder::PngEncoderId(), outputStream);

            be.SetSoftwareBitmap(softwareBitmap);

            co_await be.FlushAsync();
        }
    }


    void HL2ResearchMode::DepthSensorLoop(HL2ResearchMode* pHL2ResearchMode)
    {
        // prevent starting loop for multiple times
        if (!pHL2ResearchMode->m_depthSensorLoopStarted)
        {
            pHL2ResearchMode->m_depthSensorLoopStarted = true;
        }
        else {
            return;
        }

        pHL2ResearchMode->m_depthSensor->OpenStream();

        try 
        {
            while (pHL2ResearchMode->m_depthSensorLoopStarted)
            {
                IResearchModeSensorFrame* pDepthSensorFrame = nullptr;
                ResearchModeSensorResolution resolution;
                pHL2ResearchMode->m_depthSensor->GetNextBuffer(&pDepthSensorFrame);

                // process sensor frame
                pDepthSensorFrame->GetResolution(&resolution);
                pHL2ResearchMode->m_depthResolution = resolution;

                IResearchModeSensorDepthFrame* pDepthFrame = nullptr;
                winrt::check_hresult(pDepthSensorFrame->QueryInterface(IID_PPV_ARGS(&pDepthFrame)));

                size_t outBufferCount = 0;
                const UINT16* pDepth = nullptr;
                pDepthFrame->GetBuffer(&pDepth, &outBufferCount);
                pHL2ResearchMode->m_depthBufferSize = outBufferCount;
                size_t outAbBufferCount = 0;
                const UINT16* pAbImage = nullptr;
                pDepthFrame->GetAbDepthBuffer(&pAbImage, &outAbBufferCount);

                auto pDepthTexture = std::make_unique<uint8_t[]>(outBufferCount);
                auto pAbTexture = std::make_unique<uint8_t[]>(outAbBufferCount);
                std::vector<float> pointCloud;

                // get tracking transform
                ResearchModeSensorTimestamp timestamp;
                pDepthSensorFrame->GetTimeStamp(&timestamp);

                auto ts = PerceptionTimestampHelper::FromSystemRelativeTargetTime(HundredsOfNanoseconds(checkAndConvertUnsigned(timestamp.HostTicks)));
                auto transToWorld = pHL2ResearchMode->m_locator.TryLocateAtTimestamp(ts, pHL2ResearchMode->m_refFrame);
                if (transToWorld == nullptr)
                {
                    continue;
                }
                auto rot = transToWorld.Orientation();
                /*{
                    std::stringstream ss;
                    ss << rot.x << "," << rot.y << "," << rot.z << "," << rot.w << "\n";
                    std::string msg = ss.str();
                    std::wstring widemsg = std::wstring(msg.begin(), msg.end());
                    OutputDebugString(widemsg.c_str());
                }*/
                auto quatInDx = XMFLOAT4(rot.x, rot.y, rot.z, rot.w);
                auto rotMat = XMMatrixRotationQuaternion(XMLoadFloat4(&quatInDx));
                auto pos = transToWorld.Position();
                auto posMat = XMMatrixTranslation(pos.x, pos.y, pos.z);
                auto depthToWorld = pHL2ResearchMode->m_depthCameraPoseInvMatrix * rotMat * posMat;

                pHL2ResearchMode->mu.lock();
                auto roiCenterFloat = XMFLOAT3(pHL2ResearchMode->m_roiCenter[0], pHL2ResearchMode->m_roiCenter[1], pHL2ResearchMode->m_roiCenter[2]);
                auto roiBoundFloat = XMFLOAT3(pHL2ResearchMode->m_roiBound[0], pHL2ResearchMode->m_roiBound[1], pHL2ResearchMode->m_roiBound[2]);
                pHL2ResearchMode->mu.unlock();
                XMVECTOR roiCenter = XMLoadFloat3(&roiCenterFloat);
                XMVECTOR roiBound = XMLoadFloat3(&roiBoundFloat);
                
                UINT16 maxAbValue = 0;
                for (UINT i = 0; i < resolution.Height; i++)
                {
                    for (UINT j = 0; j < resolution.Width; j++)
                    {
                        auto idx = resolution.Width * i + j;
                        UINT16 depth = pDepth[idx];
                        depth = (depth > 4090) ? 0 : depth - pHL2ResearchMode->m_depthOffset;

                        // back-project point cloud within Roi
                        if (i > pHL2ResearchMode->depthCamRoi.kRowLower*resolution.Height&& i < pHL2ResearchMode->depthCamRoi.kRowUpper * resolution.Height &&
                            j > pHL2ResearchMode->depthCamRoi.kColLower* resolution.Width&& j < pHL2ResearchMode->depthCamRoi.kColUpper * resolution.Width &&
                            depth > pHL2ResearchMode->depthCamRoi.depthNearClip && depth < pHL2ResearchMode->depthCamRoi.depthFarClip)
                        {
                            float xy[2] = { 0, 0 };
                            float uv[2] = { j, i };
                            pHL2ResearchMode->m_pDepthCameraSensor->MapImagePointToCameraUnitPlane(uv, xy);
                            auto pointOnUnitPlane = XMFLOAT3(xy[0], xy[1], 1);
                            auto tempPoint = (float)depth / 1000 * XMVector3Normalize(XMLoadFloat3(&pointOnUnitPlane));
                            // apply transformation
                            auto pointInWorld = XMVector3Transform(tempPoint, depthToWorld);

                            // filter point cloud based on region of interest
                            if (!pHL2ResearchMode->m_useRoiFilter ||
                                (pHL2ResearchMode->m_useRoiFilter && XMVector3InBounds(pointInWorld - roiCenter, roiBound)))
                            {
                                pointCloud.push_back(XMVectorGetX(pointInWorld));
                                pointCloud.push_back(XMVectorGetY(pointInWorld));
                                pointCloud.push_back(-XMVectorGetZ(pointInWorld));
                            }
                        }

                        // save depth map as grayscale texture pixel into temp buffer
                        if (depth == 0) { pDepthTexture.get()[idx] = 0; }
                        else { pDepthTexture.get()[idx] = (uint8_t)((float)depth / 1000 * 255); }

                        // save AbImage as grayscale texture pixel into temp buffer
                        UINT16 abValue = pAbImage[idx];
                        uint8_t processedAbValue = 0;
                        if (abValue > 1000) { processedAbValue = 0xFF; }
                        else { processedAbValue = (uint8_t)((float)abValue / 1000 * 255); }

                        /*if (abValue > maxAbValue) 
                        {
                            maxAbValue = abValue;
                            std::stringstream ss;
                            ss << "Non zero valule. Idx: " << idx << " Raw Value: " << abValue << "Processed: " << (int)processedAbValue << "\n";
                            std::string msg = ss.str();
                            std::wstring widemsg = std::wstring(msg.begin(), msg.end());
                            OutputDebugString(widemsg.c_str());
                        }*/
                        pAbTexture.get()[idx] = processedAbValue;

                        // save the depth of center pixel
                        if (i == (UINT)(0.35 * resolution.Height) && j == (UINT)(0.5 * resolution.Width)
                            && pointCloud.size()>=3)
                        {
                            pHL2ResearchMode->m_centerDepth = depth;
                            if (depth > pHL2ResearchMode->depthCamRoi.depthNearClip && depth < pHL2ResearchMode->depthCamRoi.depthFarClip)
                            {
                                std::lock_guard<std::mutex> l(pHL2ResearchMode->mu);
                                pHL2ResearchMode->m_centerPoint[0] = *(pointCloud.end() - 3);
                                pHL2ResearchMode->m_centerPoint[1] = *(pointCloud.end() - 2);
                                pHL2ResearchMode->m_centerPoint[2] = *(pointCloud.end() - 1);
                            }
                        }
                    }
                }

                // save data
                {
                    std::lock_guard<std::mutex> l(pHL2ResearchMode->mu);

                    // save point cloud
                    if (!pHL2ResearchMode->m_pointCloud)
                    {
                        OutputDebugString(L"Create Space for point cloud...\n");
                        pHL2ResearchMode->m_pointCloud = new float[outBufferCount * 3];
                    }

                    memcpy(pHL2ResearchMode->m_pointCloud, pointCloud.data(), pointCloud.size() * sizeof(float));
                    pHL2ResearchMode->m_pointcloudLength = pointCloud.size();

                    // save raw depth map
                    if (!pHL2ResearchMode->m_depthMap)
                    {
                        OutputDebugString(L"Create Space for depth map...\n");
                        pHL2ResearchMode->m_depthMap = new UINT16[outBufferCount];
                    }
                    memcpy(pHL2ResearchMode->m_depthMap, pDepth, outBufferCount * sizeof(UINT16));

                    // save pre-processed depth map texture (for visualization)
                    if (!pHL2ResearchMode->m_depthMapTexture)
                    {
                        OutputDebugString(L"Create Space for depth map texture...\n");
                        pHL2ResearchMode->m_depthMapTexture = new UINT8[outBufferCount];
                    }
                    memcpy(pHL2ResearchMode->m_depthMapTexture, pDepthTexture.get(), outBufferCount * sizeof(UINT8));

                    // save raw AbImage
                    if (!pHL2ResearchMode->m_shortAbImage)
                    {
                        OutputDebugString(L"Create Space for short AbImage...\n");
                        pHL2ResearchMode->m_shortAbImage = new UINT16[outBufferCount];
                    }
                    memcpy(pHL2ResearchMode->m_shortAbImage, pAbImage, outBufferCount * sizeof(UINT16));

                    // save pre-processed AbImage texture (for visualization)
                    if (!pHL2ResearchMode->m_shortAbImageTexture)
                    {
                        OutputDebugString(L"Create Space for short AbImage texture...\n");
                        pHL2ResearchMode->m_shortAbImageTexture = new UINT8[outBufferCount];
                    }
                    memcpy(pHL2ResearchMode->m_shortAbImageTexture, pAbTexture.get(), outBufferCount * sizeof(UINT8));

                    //save color here as well...

                }
                pHL2ResearchMode->m_shortAbImageTextureUpdated = true;
                pHL2ResearchMode->m_depthMapTextureUpdated = true;
                pHL2ResearchMode->m_pointCloudUpdated = true;

                pDepthTexture.reset();

                // release space
                if (pDepthFrame) {
                    pDepthFrame->Release();
                }
                if (pDepthSensorFrame)
                {
                    pDepthSensorFrame->Release();
                }
                
            }
        }
        catch (...)  {}
        pHL2ResearchMode->m_depthSensor->CloseStream();
        pHL2ResearchMode->m_depthSensor->Release();
        pHL2ResearchMode->m_depthSensor = nullptr;
        
    }

    void HL2ResearchMode::StartLongDepthSensorLoop()
    {
        /*if (m_refFrame == nullptr)
        {
            m_refFrame = m_locator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem();
        }*/
        
        OutputDebugString(L"Starting long depth sensor loop\n");

        m_pLongDepthUpdateThread = new std::thread(HL2ResearchMode::LongDepthSensorLoop, this);
    }

    struct __declspec(uuid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")) __declspec(novtable) IMemoryBufferByteAccess : ::IUnknown
    {
        virtual HRESULT __stdcall GetBuffer(uint8_t** value, uint32_t* capacity) = 0;
    };

    void HL2ResearchMode::LongDepthSensorLoop(HL2ResearchMode* pHL2ResearchMode)
    {
        // prevent starting loop for multiple times
        if (!pHL2ResearchMode->m_longDepthSensorLoopStarted)
        {
            pHL2ResearchMode->m_longDepthSensorLoopStarted = true;
        }
        else {
            return;
        }

        //add option to not collect data until QR code detected...

        pHL2ResearchMode->m_longDepthSensor->OpenStream();
        //FILE* f = fopen("test_out.txt", "w");
        int fc = 0;

        while (pHL2ResearchMode->m_longDepthSensorLoopStarted)
        {
            //wait for color sensor loop to have also started...
            if (!pHL2ResearchMode->m_PVLoopStarted)
            {
                continue;
            }

            if (!pHL2ResearchMode->IsQRCodeDetected())
            {
                continue;
            }

            if (pHL2ResearchMode->m_refFrame == nullptr)
            {
                pHL2ResearchMode->m_refFrame = pHL2ResearchMode->m_locator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem();
            }

            IResearchModeSensorFrame* pDepthSensorFrame = nullptr;
            ResearchModeSensorResolution resolution;
            
            //let's lock before calling GetNextBuffer (which blocks this), so that other color frames don't come in
            //with different times...
            pHL2ResearchMode->mu.lock();

            //from the documentation - GetNextBuffer blocks...
            pHL2ResearchMode->m_longDepthSensor->GetNextBuffer(&pDepthSensorFrame);

            pDepthSensorFrame->GetResolution(&resolution);
            pHL2ResearchMode->m_longDepthResolution = resolution;

            if (pHL2ResearchMode->_depthPts == 0)
            {
                pHL2ResearchMode->_depthPts = new winrt::Windows::Foundation::Numerics::float3[resolution.Height * resolution.Width];
                pHL2ResearchMode->m_localDepth = new float[resolution.Height * resolution.Width * 4];
                pHL2ResearchMode->m_localDepthLength = resolution.Height * resolution.Width * 4;
                pHL2ResearchMode->m_localPointCloud = new UINT16[resolution.Height * resolution.Width * 4];
            }

            IResearchModeSensorDepthFrame* pDepthFrame = nullptr;
            winrt::check_hresult(pDepthSensorFrame->QueryInterface(IID_PPV_ARGS(&pDepthFrame)));

            size_t outBufferCount = 0;
            size_t activeBrightnessCount = 0;
            const UINT16* pDepth = nullptr;
            const UINT16* pActiveBrightness = nullptr;
            const BYTE* pSigma = nullptr;

            pDepthFrame->GetSigmaBuffer(&pSigma, &outBufferCount);
            pDepthFrame->GetAbDepthBuffer(&pActiveBrightness, &activeBrightnessCount);
            pDepthFrame->GetBuffer(&pDepth, &outBufferCount);

            pHL2ResearchMode->m_longDepthBufferSize = outBufferCount;
            ResearchModeSensorTimestamp timestamp;
            pDepthSensorFrame->GetTimeStamp(&timestamp);


            // process sensor frame

            auto pDepthTexture = std::make_unique<uint8_t[]>(outBufferCount);
            auto pDepthTextureFiltered = std::make_unique<uint16_t[]>(outBufferCount);

            std::vector<float> pointCloud;

            // get tracking transform

            //timestamp continuosly increases... but does depth buffer change?
            /*std::stringstream ss;
            ss << fc << ": " << timestamp.HostTicks << " , " << timestamp.SensorTicks << "\n";
            std::string msg = ss.str();
            std::wstring widemsg = std::wstring(msg.begin(), msg.end());
            OutputDebugString(widemsg.c_str());*/

            auto ts = PerceptionTimestampHelper::FromSystemRelativeTargetTime(HundredsOfNanoseconds(checkAndConvertUnsigned(timestamp.HostTicks)));

            auto transToWorld = pHL2ResearchMode->m_locator.TryLocateAtTimestamp(ts, pHL2ResearchMode->m_refFrame);
            if (transToWorld == nullptr)
            {
                pHL2ResearchMode->mu.unlock();
                continue;
            }

            auto rot = transToWorld.Orientation();
            /*{
                std::stringstream ss;
                ss << rot.x << "," << rot.y << "," << rot.z << "," << rot.w << "\n";
                std::string msg = ss.str();
                std::wstring widemsg = std::wstring(msg.begin(), msg.end());
                OutputDebugString(widemsg.c_str());
            }*/
            auto quatInDx = XMFLOAT4(rot.x, rot.y, rot.z, rot.w);
            auto rotMat = XMMatrixRotationQuaternion(XMLoadFloat4(&quatInDx));
            auto pos = transToWorld.Position();
            auto posMat = XMMatrixTranslation(pos.x, pos.y, pos.z);
            auto depthToWorld = pHL2ResearchMode->m_longDepthCameraPoseInvMatrix * rotMat * posMat;

            std::wstring m_datetime;
            wchar_t m_datetime_c[200];
            const std::time_t now = std::time(nullptr);
            std::tm tm;
            localtime_s(&tm, &now);
            std::wcsftime(m_datetime_c, sizeof(m_datetime_c), L"%F-%H%M%S", &tm);

            m_datetime.assign(m_datetime_c);

            wchar_t m_ms[64];
            swprintf_s(m_ms, L"%lu", GetTickCount64());

            wchar_t depthTimestampString[64];
            swprintf_s(depthTimestampString, L"%llu", timestamp.HostTicks);

            winrt::Windows::Storage::StorageFolder storageFolder = winrt::Windows::Storage::ApplicationData::Current().LocalFolder();
            auto path = storageFolder.Path().data();
            std::wstring fullName(path);

            pHL2ResearchMode->m_depthSensorPosition[0] = pos.x;
            pHL2ResearchMode->m_depthSensorPosition[1] = pos.y;
            pHL2ResearchMode->m_depthSensorPosition[2] = pos.z;

            for (int p = 0; p < 4; ++p)
            {
                for (int q = 0; q < 4; ++q)
                {
                    pHL2ResearchMode->m_depthToWorld[p * 4 + q] = depthToWorld.r[p].n128_f32[q];
                    pHL2ResearchMode->m_currRotation[p * 4 + q] = rotMat.r[p].n128_f32[q];
                    pHL2ResearchMode->m_currPosition[p * 4 + q] = posMat.r[p].n128_f32[q];
                }
            }

            if (pHL2ResearchMode->IsCapturingTransforms())
            {
                std::ofstream file;
                std::wstring pcName = fullName + L"\\" + m_datetime + L"_" + m_ms + L"_trans.txt";
                file.open(pcName);
                file << pHL2ResearchMode->m_depthToWorld[0] << " " << pHL2ResearchMode->m_depthToWorld[1] << " " << pHL2ResearchMode->m_depthToWorld[2] << " " << pHL2ResearchMode->m_depthToWorld[3] << std::endl;
                file << pHL2ResearchMode->m_depthToWorld[4] << " " << pHL2ResearchMode->m_depthToWorld[5] << " " << pHL2ResearchMode->m_depthToWorld[6] << " " << pHL2ResearchMode->m_depthToWorld[7] << std::endl;
                file << pHL2ResearchMode->m_depthToWorld[8] << " " << pHL2ResearchMode->m_depthToWorld[9] << " " << pHL2ResearchMode->m_depthToWorld[10] << " " << pHL2ResearchMode->m_depthToWorld[11] << std::endl;
                file << pHL2ResearchMode->m_depthToWorld[12] << " " << pHL2ResearchMode->m_depthToWorld[13] << " " << pHL2ResearchMode->m_depthToWorld[14] << " " << pHL2ResearchMode->m_depthToWorld[15] << std::endl;

                file.close();

                pHL2ResearchMode->_lastTransformName = hstring(pcName);
            }

            /*FILE* fLocalDepth = 0;
            if (pHL2ResearchMode->IsCapturingBinaryDepth())
            {
                std::wstring localName = fullName + +L"\\" + m_datetime + L"_" + m_ms + L"_ld.bytes";
                //std::wstring localName = fullName + +L"\\" + m_datetime + L"_" + m_ms + L"_ld.png";
                _wfopen_s(&fLocalDepth, localName.c_str(), L"wb");

                pHL2ResearchMode->_lastBinaryDepthName = hstring(localName);
            }*/

            for (UINT i = 0; i < resolution.Height; i++)
            {
                for (UINT j = 0; j < resolution.Width; j++)
                {
                    UINT idx = resolution.Width * i + j;
                    UINT16 depth = pDepth[idx];
                    depth = (pSigma[idx] & 0x80) ? 0 : depth - pHL2ResearchMode->m_depthOffset;


                    float xy[2] = { 0, 0 };
                    float uv[2] = { ((float)j), ((float)i)};
                    float z = 1.0f;
                    float zL = 1.0f;
                    float zR = 1.0f;

                    float xyL[2] = { 0, 0 };
                    float xyR[2] = { 0, 0 };
                    float uvL[2] = { ((float)j) - 0.5f, ((float)i) };
                    float uvR[2] = { ((float)j) + 0.5f, ((float)i) };
                    HRESULT hr = pHL2ResearchMode->m_pLongDepthCameraSensor->MapImagePointToCameraUnitPlane(uv, xy);
                    
                    //auto pointOnUnitPlane = XMFLOAT3(xy[0], xy[1], 1);
                    if (FAILED(hr))
                    {
                        z = 0.0f;
                        pHL2ResearchMode->_depthPts[idx].x = 0.0f;
                        pHL2ResearchMode->_depthPts[idx].y = 0.0f;
                        pHL2ResearchMode->_depthPts[idx].z = 0.0f;

                        pHL2ResearchMode->m_localDepth[idx*4] = 0.0f;
                        pHL2ResearchMode->m_localDepth[idx*4+1] = 0.0f;
                        pHL2ResearchMode->m_localDepth[idx*4+2] = 0.0f;
                        pHL2ResearchMode->m_localDepth[idx * 4 + 3] = 0.0f;

                        if (pHL2ResearchMode->IsCapturingBinaryDepth())
                        {
                            pHL2ResearchMode->m_localPointCloud[idx * 4] = 0;
                            pHL2ResearchMode->m_localPointCloud[idx * 4 + 1] = 0;
                            pHL2ResearchMode->m_localPointCloud[idx * 4 + 2] = 0;
                            pHL2ResearchMode->m_localPointCloud[idx * 4 + 3] = 65535;
                        }
                        continue;
                    }

                    const float norm = sqrtf(xy[0] * xy[0] + xy[1] * xy[1] + z * z);
                    if (norm > 0.0f)
                    {
                        const float invNorm = 1.0f / norm;
                        xy[0] *= invNorm;
                        xy[1] *= invNorm;
                        z *= invNorm;
                    }
                    else
                    {
                        z = 0.0f;
                    }

                    float d = (float)depth/1000.0f;
                    XMFLOAT3 tempPoint = XMFLOAT3(xy[0] * d, xy[1] * d, z*d);
                    pHL2ResearchMode->m_localDepth[idx*4] = tempPoint.x;
                    pHL2ResearchMode->m_localDepth[idx*4 + 1] = tempPoint.y;
                    pHL2ResearchMode->m_localDepth[idx*4 + 2] = tempPoint.z;
                    pHL2ResearchMode->m_localDepth[idx*4 + 3] = 0.0f;

                    if (pHL2ResearchMode->IsCapturingBinaryDepth())
                    {
                        pHL2ResearchMode->m_localPointCloud[idx * 4] = (UINT16)(tempPoint.x * 1000.0f + 32768);
                        pHL2ResearchMode->m_localPointCloud[idx * 4 + 1] = (UINT16)(tempPoint.y * 1000.0f + 32768);
                        pHL2ResearchMode->m_localPointCloud[idx * 4 + 2] = (UINT16)(tempPoint.z * 1000.0f + 32768);
                        pHL2ResearchMode->m_localPointCloud[idx * 4 + 3] = 65535;
                    }

                    HRESULT hrL = pHL2ResearchMode->m_pLongDepthCameraSensor->MapImagePointToCameraUnitPlane(uvL, xyL);
                    HRESULT hrR = pHL2ResearchMode->m_pLongDepthCameraSensor->MapImagePointToCameraUnitPlane(uvR, xyR);

                    if (!FAILED(hrL) && !FAILED(hrR))
                    {
                        float normL = sqrtf(xyL[0] * xyL[0] + xyL[1] * xyL[1] + zL * zL);
                        if (normL > 0.0f)
                        {
                            const float invNorm = 1.0f / normL;
                            xyL[0] *= invNorm;
                            xyL[1] *= invNorm;
                            zL *= invNorm;
                        }
                        else
                        {
                            zL = 0.0f;
                        }

                        XMFLOAT3 tempPointL = XMFLOAT3(xyL[0] * d, xyL[1] * d, zL * d);

                        float normR = sqrtf(xyR[0] * xyR[0] + xyR[1] * xyR[1] + zR * zR);
                        if (normR > 0.0f)
                        {
                            const float invNorm = 1.0f / normR;
                            xyR[0] *= invNorm;
                            xyR[1] *= invNorm;
                            zR *= invNorm;
                        }
                        else
                        {
                            zR = 0.0f;
                        }

                        XMFLOAT3 tempPointR = XMFLOAT3(xyR[0] * d, xyR[1] * d, zR * d);

                        float dX = (tempPointL.x - tempPointR.x) * (tempPointL.x - tempPointR.x);
                        float dY = (tempPointL.y - tempPointR.y) * (tempPointL.y - tempPointR.y);
                        float dZ = (tempPointL.z - tempPointR.z) * (tempPointL.z - tempPointR.z);

                        pHL2ResearchMode->m_localDepth[idx * 4 + 3] = sqrtf(dX + dY + dZ);
                    }

                    /*if (pHL2ResearchMode->IsCapturingBinaryDepth())
                    {
                        fwrite(&(pHL2ResearchMode->m_localDepth[idx * 4]), sizeof(float), 1, fLocalDepth);
                        fwrite(&(pHL2ResearchMode->m_localDepth[idx * 4 + 1]), sizeof(float), 1, fLocalDepth);
                        fwrite(&(pHL2ResearchMode->m_localDepth[idx * 4 + 2]), sizeof(float), 1, fLocalDepth);
                        fwrite(&(pHL2ResearchMode->m_localDepth[idx * 4 + 3]), sizeof(float), 1, fLocalDepth);
                    }*/

                    auto pointInWorld = XMVector3Transform(XMLoadFloat3(&tempPoint), depthToWorld);

                    pHL2ResearchMode->_depthPts[idx].x = pointInWorld.n128_f32[0];// /= pointInWorld.n128_f32[3];
                    pHL2ResearchMode->_depthPts[idx].y = pointInWorld.n128_f32[1];// /= pointInWorld.n128_f32[3];
                    pHL2ResearchMode->_depthPts[idx].z = pointInWorld.n128_f32[2];// /= pointInWorld.n128_f32[3];


                    // save as grayscale texture pixel into temp buffer
                    if (depth == 0) {
                        pDepthTexture.get()[idx] = 0;
                        pDepthTextureFiltered.get()[idx] = 0;
                    }

                    else {
                        pDepthTexture.get()[idx] = (uint8_t)((float)depth / 4000 * 255);
                        pDepthTextureFiltered.get()[idx] = depth;
                    }
                }
            }
            
           /* if (pHL2ResearchMode->IsCapturingBinaryDepth())
            {
                if (fLocalDepth != 0)
                {
                    fclose(fLocalDepth);
                }
            }*/
            
            winrt::Windows::Foundation::Numerics::float4x4 worldToPV = winrt::Windows::Foundation::Numerics::float4x4::identity();

            if (pHL2ResearchMode->m_latestFrame != nullptr)
            {
                winrt::Windows::Media::Capture::Frames::MediaFrameReference frame = pHL2ResearchMode->m_latestFrame;

                long long ts = pHL2ResearchMode->m_converter.RelativeTicksToAbsoluteTicks(HundredsOfNanoseconds(frame.SystemRelativeTime().Value().count())).count();
                
                winrt::Windows::Foundation::Numerics::float4x4 PVtoWorldtransform;
                winrt::Windows::Foundation::Numerics::float4x4 worldToPVtransform;

                auto PVtoWorld =
                    frame.CoordinateSystem().TryGetTransformTo(pHL2ResearchMode->m_refFrame);
                    
                if (PVtoWorld)
                {
                    PVtoWorldtransform = PVtoWorld.Value();
                    //std::lock_guard<std::mutex> l(pHL2ResearchMode->mu);
                    pHL2ResearchMode->m_PVToWorld[0] = PVtoWorldtransform.m11;
                    pHL2ResearchMode->m_PVToWorld[1] = PVtoWorldtransform.m12;
                    pHL2ResearchMode->m_PVToWorld[2] = PVtoWorldtransform.m13;
                    pHL2ResearchMode->m_PVToWorld[3] = PVtoWorldtransform.m14;

                        
                    pHL2ResearchMode->m_PVToWorld[4] = PVtoWorldtransform.m21;
                    pHL2ResearchMode->m_PVToWorld[5] = PVtoWorldtransform.m22;
                    pHL2ResearchMode->m_PVToWorld[6] = PVtoWorldtransform.m23;
                    pHL2ResearchMode->m_PVToWorld[7] = PVtoWorldtransform.m24;

                    pHL2ResearchMode->m_PVToWorld[8] = PVtoWorldtransform.m31;
                    pHL2ResearchMode->m_PVToWorld[9] = PVtoWorldtransform.m32;
                    pHL2ResearchMode->m_PVToWorld[10] = PVtoWorldtransform.m33;
                    pHL2ResearchMode->m_PVToWorld[11] = PVtoWorldtransform.m34;

                    pHL2ResearchMode->m_PVToWorld[12] = PVtoWorldtransform.m41;
                    pHL2ResearchMode->m_PVToWorld[13] = PVtoWorldtransform.m42;
                    pHL2ResearchMode->m_PVToWorld[14] = PVtoWorldtransform.m43;
                    pHL2ResearchMode->m_PVToWorld[15] = PVtoWorldtransform.m44;

                    pHL2ResearchMode->m_PVCameraPose._11 = PVtoWorldtransform.m11;
                    pHL2ResearchMode->m_PVCameraPose._12 = PVtoWorldtransform.m12;
                    pHL2ResearchMode->m_PVCameraPose._13 = PVtoWorldtransform.m13;
                    pHL2ResearchMode->m_PVCameraPose._14 = PVtoWorldtransform.m14;
                    pHL2ResearchMode->m_PVCameraPose._21 = PVtoWorldtransform.m21;
                    pHL2ResearchMode->m_PVCameraPose._22 = PVtoWorldtransform.m22;
                    pHL2ResearchMode->m_PVCameraPose._23 = PVtoWorldtransform.m23;
                    pHL2ResearchMode->m_PVCameraPose._24 = PVtoWorldtransform.m24;
                    pHL2ResearchMode->m_PVCameraPose._31 = PVtoWorldtransform.m31;
                    pHL2ResearchMode->m_PVCameraPose._32 = PVtoWorldtransform.m32;
                    pHL2ResearchMode->m_PVCameraPose._33 = PVtoWorldtransform.m33;
                    pHL2ResearchMode->m_PVCameraPose._34 = PVtoWorldtransform.m34;
                    pHL2ResearchMode->m_PVCameraPose._41 = PVtoWorldtransform.m41;
                    pHL2ResearchMode->m_PVCameraPose._42 = PVtoWorldtransform.m42;
                    pHL2ResearchMode->m_PVCameraPose._43 = PVtoWorldtransform.m43;
                    pHL2ResearchMode->m_PVCameraPose._44 = PVtoWorldtransform.m44;

                    pHL2ResearchMode->m_PVCameraPoseInvMatrix = XMMatrixInverse(nullptr, XMLoadFloat4x4(&pHL2ResearchMode->m_PVCameraPose));
                    DirectX::XMFLOAT4X4 pvCamToWorld;
                    XMStoreFloat4x4(&pvCamToWorld, pHL2ResearchMode->m_PVCameraPoseInvMatrix);

                    worldToPVtransform.m11 = pvCamToWorld._11;
                    worldToPVtransform.m12 = pvCamToWorld._12;
                    worldToPVtransform.m13 = pvCamToWorld._13;
                    worldToPVtransform.m14 = pvCamToWorld._14;
                    worldToPVtransform.m21 = pvCamToWorld._21;
                    worldToPVtransform.m22 = pvCamToWorld._22;
                    worldToPVtransform.m23 = pvCamToWorld._23;
                    worldToPVtransform.m24 = pvCamToWorld._24;
                    worldToPVtransform.m31 = pvCamToWorld._31;
                    worldToPVtransform.m32 = pvCamToWorld._32;
                    worldToPVtransform.m33 = pvCamToWorld._33;
                    worldToPVtransform.m34 = pvCamToWorld._34;
                    worldToPVtransform.m41 = pvCamToWorld._41;
                    worldToPVtransform.m42 = pvCamToWorld._42;
                    worldToPVtransform.m43 = pvCamToWorld._43;
                    worldToPVtransform.m44 = pvCamToWorld._44;

                    //_PVtoWorldtransform = PVtoWorldtransform;
                }
                else
                {
                    OutputDebugStringW(L"Streamer::SendFrame: Could not locate frame.\n");
                    pHL2ResearchMode->mu.unlock();
                    return;
                }

                // grab the frame data
                winrt::Windows::Graphics::Imaging::SoftwareBitmap softwareBitmap = winrt::Windows::Graphics::Imaging::SoftwareBitmap::Convert(
                    frame.VideoMediaFrame().SoftwareBitmap(), winrt::Windows::Graphics::Imaging::BitmapPixelFormat::Bgra8);
                    
                if (pHL2ResearchMode->IsCapturingHiResColor())
                {
                    wchar_t fName[128];
                    //wchar_t fDate[64];
                    //swprintf(fDate, 64, L"%ld", ts);
                    swprintf(fName, 128, L"%s_%s_hicolor.png", m_datetime.c_str(), m_ms);
                    //std::wstring pcName = fullName + L"\\" + m_datetime + L"_" + m_ms + L"_hicolor.png";
                    winrt::Windows::Storage::StorageFolder storageFolder = winrt::Windows::Storage::ApplicationData::Current().LocalFolder();
                    pHL2ResearchMode->_lastHiColorName = storageFolder.Path();
                    pHL2ResearchMode->_lastHiColorName = pHL2ResearchMode->_lastHiColorName + hstring(L"\\") + hstring(fName);

                    CreateLocalFile(fName, softwareBitmap);
                    if (!pHL2ResearchMode->IsCapturingRectColor())
                    {
                        pHL2ResearchMode->_frameCount++;
                    }
                }

                //frameCount++;

                int imageWidth = softwareBitmap.PixelWidth();
                int imageHeight = softwareBitmap.PixelHeight();

                int pixelStride = 4;
                int scaleFactor = 1;

                int rowStride = imageWidth * pixelStride;

                // Get bitmap buffer object of the frame
                winrt::Windows::Graphics::Imaging::BitmapBuffer bitmapBuffer = softwareBitmap.LockBuffer(winrt::Windows::Graphics::Imaging::BitmapBufferAccessMode::Read);

                // Get raw pointer to the buffer object
                uint32_t pixelBufferDataLength = 0;
                uint8_t* pixelBufferData;

                auto spMemoryBufferByteAccess{ bitmapBuffer.CreateReference()
                    .as<::Windows::Foundation::IMemoryBufferByteAccess>() };

                try
                {
                    spMemoryBufferByteAccess->
                        GetBuffer(&pixelBufferData, &pixelBufferDataLength);
                }
                catch (winrt::hresult_error const& ex)
                {
                    winrt::hresult hr = ex.code(); // HRESULT_FROM_WIN32
                    winrt::hstring message = ex.message();
                    OutputDebugStringW(L"VideoCameraStreamer::SendFrame: Failed to get buffer with ");
                    OutputDebugStringW(message.c_str());
                    OutputDebugStringW(L"\n");
                }

                //pHL2ResearchMode->mu.lock();
                if (pHL2ResearchMode->m_pixelBufferData == 0)
                {
                    pHL2ResearchMode->m_pixelBufferData = new UINT8[pixelBufferDataLength];
                    pHL2ResearchMode->m_colorBufferSize = pixelBufferDataLength;
                }

                memcpy(pHL2ResearchMode->m_pixelBufferData, pixelBufferData, pHL2ResearchMode->m_colorBufferSize); 

                std::vector<winrt::Windows::Foundation::Numerics::float3> camPoints;
                std::vector< winrt::Windows::Foundation::Point> screenPoints;

                for (UINT i = 0; i < resolution.Height; i++)
                {
                    for (UINT j = 0; j < resolution.Width; j++)
                    {
                        UINT idx = resolution.Width * i + j;
                        
                        XMFLOAT3 depthVec;
                        depthVec.x = pHL2ResearchMode->_depthPts[idx].x;
                        depthVec.y = pHL2ResearchMode->_depthPts[idx].y;
                        depthVec.z = pHL2ResearchMode->_depthPts[idx].z;
                        //depthVec.w = pHL2ResearchMode->_depthPts[idx].w;
                        XMVECTOR pointInPVCam = XMVector3Transform(XMLoadFloat3(&depthVec), pHL2ResearchMode->m_PVCameraPoseInvMatrix);
                        winrt::Windows::Foundation::Numerics::float3 cp;
                        cp.x = pointInPVCam.n128_f32[0];
                        cp.y = pointInPVCam.n128_f32[1];
                        cp.z = (pointInPVCam.n128_f32[2]);
                        camPoints.push_back(cp);
                        winrt::Windows::Foundation::Point p;
                        p.X = 0;
                        p.Y = 0;
                        screenPoints.push_back(p);
                    }
                }

                winrt::array_view<winrt::Windows::Foundation::Numerics::float3> camPointsView{ camPoints };

                winrt::array_view<winrt::Windows::Foundation::Point> screenPointsView{ screenPoints };

                frame.VideoMediaFrame().CameraIntrinsics().ProjectManyOntoFrame(camPointsView, screenPointsView);

                std::ofstream file;
                std::wstring pcName = fullName + +L"\\" + m_datetime + L"_" + m_ms + L"_pc.txt";
                if (pHL2ResearchMode->IsCapturingColoredPointCloud())
                {
                    file.open(pcName);
                }
                
                {
                    winrt::Windows::Graphics::Imaging::SoftwareBitmap rectColor = winrt::Windows::Graphics::Imaging::SoftwareBitmap(winrt::Windows::Graphics::Imaging::BitmapPixelFormat::Bgra8, 320, 288, winrt::Windows::Graphics::Imaging::BitmapAlphaMode::Premultiplied);
                    winrt::Windows::Graphics::Imaging::BitmapBuffer buffer = rectColor.LockBuffer(winrt::Windows::Graphics::Imaging::BitmapBufferAccessMode::Write);
                    winrt::Windows::Foundation::IMemoryBufferReference reference = buffer.CreateReference();

                    byte* dataInBytes;
                    unsigned int capacity;
                    reference.as<::Windows::Foundation::IMemoryBufferByteAccess>()->GetBuffer(&dataInBytes, &capacity);

                    // Fill-in the BGRA plane
                    winrt::Windows::Graphics::Imaging::BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);


                    //INT totalImageSize = imageWidth * 4 * imageHeight;
                    //iterate through screenpointsview and lookup colors...
                    for (UINT i = 0; i < resolution.Height; i++)
                    {
                        UINT wIdx = resolution.Width * i;
                        for (UINT j = 0; j < resolution.Width; j++)
                        {
                            UINT idx = wIdx + j;

                            float fX = screenPointsView[idx].X; // imageWidth;
                            float fY = screenPointsView[idx].Y; // imageHeight;

                            if (fX >= 0.0f && fX < imageWidth && fY >= 0.0f && fY < imageHeight)
                            {
                                INT cIdx = (INT)((imageWidth * 4) * (INT)(imageHeight - fY)) + (INT)(imageWidth - 1 - fX) * 4;

                                if (cIdx > 0 && cIdx < (INT)(imageWidth * 4 * imageHeight))
                                {

                                    UINT8 r = pHL2ResearchMode->m_pixelBufferData[cIdx + 2];// imageBufferAsVector[cIdx + 2];// ;
                                    UINT8 g = pHL2ResearchMode->m_pixelBufferData[cIdx + 1]; //imageBufferAsVector[cIdx + 1]; //
                                    UINT8 b = pHL2ResearchMode->m_pixelBufferData[cIdx];//imageBufferAsVector[cIdx]; //
                                    UINT8 a = pHL2ResearchMode->m_pixelBufferData[cIdx + 3]; //imageBufferAsVector[cIdx + 3]; //

                                    if (pHL2ResearchMode->IsCapturingRectColor())
                                    {
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = b;
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = g;
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = r;
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = a;
                                    }

                                    pointCloud.push_back(pHL2ResearchMode->_depthPts[idx].x);
                                    pointCloud.push_back(pHL2ResearchMode->_depthPts[idx].y);
                                    pointCloud.push_back(pHL2ResearchMode->_depthPts[idx].z);
                                    pointCloud.push_back((float)r / 255.0f);
                                    pointCloud.push_back((float)g / 255.0f);
                                    pointCloud.push_back((float)b / 255.0f);
                                    if (file.is_open() && pHL2ResearchMode->IsCapturingColoredPointCloud())
                                    {
                                        file << pHL2ResearchMode->_depthPts[idx].x << " " << pHL2ResearchMode->_depthPts[idx].y << " " << pHL2ResearchMode->_depthPts[idx].z << " " << ((float)r / 255.0f) << " " << (float)g / 255.0f << " " << (float)b / 255.0f << std::endl;
                                    }
                                }
                                else
                                {
                                    pointCloud.push_back(0.0f);
                                    pointCloud.push_back(0.0f);
                                    pointCloud.push_back(0.0f);
                                    pointCloud.push_back(0.0f);
                                    pointCloud.push_back(0.0f);
                                    pointCloud.push_back(0.0f);

                                    if (pHL2ResearchMode->IsCapturingRectColor())
                                    {
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = 0;
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = 0;
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = 0;
                                        dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = 255;
                                    }
                                }
                            }
                            else
                            {
                                //could keep the point cloud data here and assign arbitray color (as no color from high-res photo matches up with field of view from depth image).
                                /*pointCloud.push_back(pHL2ResearchMode->_depthPts[idx].x);
                                pointCloud.push_back(pHL2ResearchMode->_depthPts[idx].y);
                                pointCloud.push_back(pHL2ResearchMode->_depthPts[idx].z);*/
                                pointCloud.push_back(0.0f);
                                pointCloud.push_back(0.0f);
                                pointCloud.push_back(0.0f);
                                pointCloud.push_back(0.0f);
                                pointCloud.push_back(0.0f);
                                pointCloud.push_back(0.0f);

                                if (pHL2ResearchMode->IsCapturingRectColor())
                                {
                                    dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = 0;
                                    dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = 0;
                                    dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = 0;
                                    dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = 255;
                                }
                            }
                        }
                    }

                    if (file.is_open() && pHL2ResearchMode->IsCapturingColoredPointCloud())
                    {
                        file.close();
                        pHL2ResearchMode->_lastPointCloudName = hstring(pcName);
                        //OutputDebugString(pcName.c_str());
                    }

                    if (pHL2ResearchMode->IsCapturingRectColor())
                    {
                        wchar_t fName[128];
                        //wchar_t fDate[64];
                        //swprintf(fDate, 64, L"%ld", ts);
                        swprintf(fName, 128, L"%s_%s_color.png", m_datetime.c_str(), m_ms);
                        //std::wstring pcName = fullName + L"\\" + m_datetime + L"_" + m_ms + L"_color.png";
                        winrt::Windows::Storage::StorageFolder storageFolder = winrt::Windows::Storage::ApplicationData::Current().LocalFolder();
                        pHL2ResearchMode->_lastRectColorName = storageFolder.Path();
                        pHL2ResearchMode->_lastRectColorName = pHL2ResearchMode->_lastRectColorName + hstring(L"\\") + hstring(fName);

                        CreateLocalFile(fName, rectColor);
                        pHL2ResearchMode->_frameCount++;
                    }
                }

                winrt::Windows::Graphics::Imaging::SoftwareBitmap depthImage = winrt::Windows::Graphics::Imaging::SoftwareBitmap(winrt::Windows::Graphics::Imaging::BitmapPixelFormat::Rgba16, 320, 288, winrt::Windows::Graphics::Imaging::BitmapAlphaMode::Straight);
                //winrt::Windows::Graphics::Imaging

                {    
                    winrt::Windows::Graphics::Imaging::BitmapBuffer bufferDepth = depthImage.LockBuffer(winrt::Windows::Graphics::Imaging::BitmapBufferAccessMode::Write);
                    winrt::Windows::Foundation::IMemoryBufferReference referenceDepth = bufferDepth.CreateReference();

                    byte* dataInBytesDepth;
                    unsigned int capacityDepth;
                    referenceDepth.as<::Windows::Foundation::IMemoryBufferByteAccess>()->GetBuffer(&dataInBytesDepth, &capacityDepth);

                    // Fill-in the BGRA plane
                    winrt::Windows::Graphics::Imaging::BitmapPlaneDescription bufferLayoutDepth = bufferDepth.GetPlaneDescription(0);

                    if (!pHL2ResearchMode->IsRectifyingImages())
                    {
                        if (pHL2ResearchMode->IsCapturingDepthImages())
                        {
                            for (UINT i = 0; i < resolution.Height; i++)
                            {
                                UINT wIdx = resolution.Width * i;
                                for (UINT j = 0; j < resolution.Width; j++)
                                {
                                    UINT idx = wIdx + j;
                                    UINT16 depth = pDepth[idx];
                                    //depth = (pSigma[idx] & 0x80) ? 0 : depth;
                                    //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = (depth & 0x00FF);
                                    //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = ((depth && 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = (depth & 0x00FF);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = ((depth & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = (depth & 0x00FF);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = ((depth & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = (depth & 0x00FF);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = ((depth & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// (depth & 0x00FF);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (UINT i = 0; i < resolution.Height; i++)
                        {
                            UINT wIdx = resolution.Width * i;
                            for (UINT j = 0; j < resolution.Width; j++)
                            {
                                UINT idx = wIdx + j;

                                float fX = screenPointsView[idx].X; // imageWidth;
                                float fY = screenPointsView[idx].Y; // imageHeight;

                                if (fX >= 0.0f && fX < imageWidth && fY >= 0.0f && fY < imageHeight)
                                {
                                    INT cIdx = (INT)((imageWidth * 4) * (INT)(imageHeight - fY)) + (INT)(imageWidth - 1 - fX) * 4;

                                    if (cIdx > 0 && cIdx < (INT)(imageWidth * 4 * imageHeight))
                                    {
                                        if (pHL2ResearchMode->IsCapturingDepthImages())
                                        {
                                            //UINT idx2 = bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + j;
                                            UINT16 depth = pDepth[idx];
                                            depth = (pSigma[idx] & 0x80) ? 0 : depth;
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = (depth & 0x00FF);
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = ((depth && 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = (depth & 0x00FF);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = (depth & 0x00FF);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = (depth & 0x00FF);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// (depth & 0x00FF);
                                        }
                                    }
                                    else
                                    {
                                        if (pHL2ResearchMode->IsCapturingDepthImages())
                                        {
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = 0;
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// ((depth & 0xFF00) >> 8);
                                        }
                                    }
                                }
                                else
                                {
                                    if (pHL2ResearchMode->IsCapturingDepthImages())
                                    {
                                        //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = 0;
                                        //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// ((depth & 0xFF00) >> 8);
                                    }
                                }
                            }
                        }
                    }
                }

                if (pHL2ResearchMode->IsCapturingDepthImages())
                {
                    wchar_t fName[128];
                    swprintf(fName, 128, L"%s_%s_depth.png", m_datetime.c_str(), m_ms);// depthTimestampString);
                    //depthImage = winrt::Windows::Graphics::Imaging::SoftwareBitmap::Convert(depthImage, winrt::Windows::Graphics::Imaging::BitmapPixelFormat::Rgba16);
                    //std::wstring pcName = fullName + L"\\" + m_datetime + L"_" + m_ms + L"_color.png";
                    winrt::Windows::Storage::StorageFolder storageFolder = winrt::Windows::Storage::ApplicationData::Current().LocalFolder();
                    pHL2ResearchMode->_lastDepthImageName = storageFolder.Path();
                    pHL2ResearchMode->_lastDepthImageName = pHL2ResearchMode->_lastDepthImageName + hstring(L"\\") + hstring(fName);

                    CreateLocalFile(fName, depthImage);
                    //pHL2ResearchMode->_frameCount++;
                }

                
                winrt::Windows::Graphics::Imaging::SoftwareBitmap intensityImage = winrt::Windows::Graphics::Imaging::SoftwareBitmap(winrt::Windows::Graphics::Imaging::BitmapPixelFormat::Rgba16, 320, 288, winrt::Windows::Graphics::Imaging::BitmapAlphaMode::Straight);
                //winrt::Windows::Graphics::Imaging

                {
                    winrt::Windows::Graphics::Imaging::BitmapBuffer bufferIntensity = intensityImage.LockBuffer(winrt::Windows::Graphics::Imaging::BitmapBufferAccessMode::Write);
                    winrt::Windows::Foundation::IMemoryBufferReference referenceDepth = bufferIntensity.CreateReference();

                    byte* dataInBytesDepth;
                    unsigned int capacityIntensity;
                    referenceDepth.as<::Windows::Foundation::IMemoryBufferByteAccess>()->GetBuffer(&dataInBytesDepth, &capacityIntensity);

                    // Fill-in the BGRA plane
                    winrt::Windows::Graphics::Imaging::BitmapPlaneDescription bufferLayoutDepth = bufferIntensity.GetPlaneDescription(0);
                    
                    if (!pHL2ResearchMode->IsRectifyingImages())
                    {
                        if (pHL2ResearchMode->IsCapturingIntensity())
                        {
                            for (UINT i = 0; i < resolution.Height; i++)
                            {
                                UINT wIdx = resolution.Width * i;
                                for (UINT j = 0; j < resolution.Width; j++)
                                {
                                    UINT idx = wIdx + j;

                                    //UINT idx2 = bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + j;
                                    UINT16 depth = pActiveBrightness[idx];
                                    //depth = (pSigma[idx] & 0x80) ? 0 : depth;
                                    //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = (depth & 0x00FF);
                                    //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = ((depth && 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = (depth & 0x00FF);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = ((depth & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = (depth & 0x00FF);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = ((depth & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = (depth & 0x00FF);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = ((depth & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// (depth & 0x00FF);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (UINT i = 0; i < resolution.Height; i++)
                        {
                            UINT wIdx = resolution.Width * i;
                            for (UINT j = 0; j < resolution.Width; j++)
                            {
                                UINT idx = wIdx + j;

                                float fX = screenPointsView[idx].X; // imageWidth;
                                float fY = screenPointsView[idx].Y; // imageHeight;

                                if (fX >= 0.0f && fX < imageWidth && fY >= 0.0f && fY < imageHeight)
                                {
                                    INT cIdx = (INT)((imageWidth * 4) * (INT)(imageHeight - fY)) + (INT)(imageWidth - 1 - fX) * 4;

                                    if (cIdx > 0 && cIdx < (INT)(imageWidth * 4 * imageHeight))
                                    {
                                        if (pHL2ResearchMode->IsCapturingIntensity())
                                        {
                                            //UINT idx2 = bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + j;
                                            UINT16 depth = pActiveBrightness[idx];
                                            //depth = (pSigma[idx] & 0x80) ? 0 : depth;
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = (depth & 0x00FF);
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = ((depth && 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = (depth & 0x00FF);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = (depth & 0x00FF);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = (depth & 0x00FF);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// (depth & 0x00FF);
                                        }
                                    }
                                    else
                                    {
                                        if (pHL2ResearchMode->IsCapturingIntensity())
                                        {
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = 0;
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// ((depth & 0xFF00) >> 8);
                                        }
                                    }
                                }
                                else
                                {
                                    if (pHL2ResearchMode->IsCapturingIntensity())
                                    {
                                        //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = 0;
                                        //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// ((depth & 0xFF00) >> 8);
                                    }
                                }
                            }
                        }
                    }
                }

                if (pHL2ResearchMode->IsCapturingIntensity())
                {
                    wchar_t fName[128];
                    swprintf(fName, 128, L"%s_%s_intensity.png", m_datetime.c_str(), m_ms);// depthTimestampString);
                    //depthImage = winrt::Windows::Graphics::Imaging::SoftwareBitmap::Convert(depthImage, winrt::Windows::Graphics::Imaging::BitmapPixelFormat::Rgba16);
                    //std::wstring pcName = fullName + L"\\" + m_datetime + L"_" + m_ms + L"_color.png";
                    winrt::Windows::Storage::StorageFolder storageFolder = winrt::Windows::Storage::ApplicationData::Current().LocalFolder();
                    pHL2ResearchMode->_lastIntensityImageName = storageFolder.Path();
                    pHL2ResearchMode->_lastIntensityImageName = pHL2ResearchMode->_lastIntensityImageName + hstring(L"\\") + hstring(fName);

                    CreateLocalFile(fName, intensityImage);
                    //pHL2ResearchMode->_frameCount++;
                }

                winrt::Windows::Graphics::Imaging::SoftwareBitmap localPCImage = winrt::Windows::Graphics::Imaging::SoftwareBitmap(winrt::Windows::Graphics::Imaging::BitmapPixelFormat::Rgba16, 320, 288, winrt::Windows::Graphics::Imaging::BitmapAlphaMode::Straight);
                //winrt::Windows::Graphics::Imaging

                {
                    winrt::Windows::Graphics::Imaging::BitmapBuffer bufferDepth = localPCImage.LockBuffer(winrt::Windows::Graphics::Imaging::BitmapBufferAccessMode::Write);
                    winrt::Windows::Foundation::IMemoryBufferReference referenceDepth = bufferDepth.CreateReference();

                    byte* dataInBytesDepth;
                    unsigned int capacityDepth;
                    referenceDepth.as<::Windows::Foundation::IMemoryBufferByteAccess>()->GetBuffer(&dataInBytesDepth, &capacityDepth);

                    // Fill-in the BGRA plane
                    winrt::Windows::Graphics::Imaging::BitmapPlaneDescription bufferLayoutDepth = bufferDepth.GetPlaneDescription(0);
                    if (!pHL2ResearchMode->IsRectifyingImages())
                    {
                        if (pHL2ResearchMode->IsCapturingBinaryDepth())
                        {
                            for (UINT i = 0; i < resolution.Height; i++)
                            {
                                UINT wIdx = resolution.Width * i;
                                for (UINT j = 0; j < resolution.Width; j++)
                                {
                                    UINT idx = wIdx + j;
                                    UINT pcIndex = wIdx * 4 + j * 4;
                                    UINT16 depthX = pHL2ResearchMode->m_localPointCloud[pcIndex];
                                    UINT16 depthY = pHL2ResearchMode->m_localPointCloud[pcIndex + 1];
                                    UINT16 depthZ = pHL2ResearchMode->m_localPointCloud[pcIndex + 2];
                                    if (pSigma[idx] & 0x80)
                                    {
                                        //depthX = 0;
                                        //depthY = 0;
                                        //depthZ = 0;
                                    }

                                    //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = (depth & 0x00FF);
                                    //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = ((depth && 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = (depthX & 0x00FF);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = ((depthX & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = (depthY & 0x00FF);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = ((depthY & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = (depthZ & 0x00FF);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = ((depthZ & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                    dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// (depth & 0x00FF);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (UINT i = 0; i < resolution.Height; i++)
                        {
                            UINT wIdx = resolution.Width * i;
                            for (UINT j = 0; j < resolution.Width; j++)
                            {
                                UINT idx = wIdx + j;

                                float fX = screenPointsView[idx].X; // imageWidth;
                                float fY = screenPointsView[idx].Y; // imageHeight;

                                if (fX >= 0.0f && fX < imageWidth && fY >= 0.0f && fY < imageHeight)
                                {
                                    INT cIdx = (INT)((imageWidth * 4) * (INT)(imageHeight - fY)) + (INT)(imageWidth - 1 - fX) * 4;

                                    if (cIdx > 0 && cIdx < (INT)(imageWidth * 4 * imageHeight))
                                    {
                                        if (pHL2ResearchMode->IsCapturingBinaryDepth())
                                        {
                                            //UINT idx2 = bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + j;
                                            UINT pcIndex = wIdx * 4 + j * 4;
                                            UINT16 depthX = pHL2ResearchMode->m_localPointCloud[pcIndex];
                                            UINT16 depthY = pHL2ResearchMode->m_localPointCloud[pcIndex + 1];
                                            UINT16 depthZ = pHL2ResearchMode->m_localPointCloud[pcIndex + 2];
                                            if (pSigma[idx] & 0x80)
                                            {
                                                //depthX = 0;
                                                //depthY = 0;
                                                //depthZ = 0;
                                            }

                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = (depth & 0x00FF);
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = ((depth && 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = (depthX & 0x00FF);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = ((depthX & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = (depthY & 0x00FF);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = ((depthY & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = (depthZ & 0x00FF);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = ((depthZ & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// (depth & 0x00FF);
                                        }
                                    }
                                    else
                                    {
                                        if (pHL2ResearchMode->IsCapturingBinaryDepth())
                                        {
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = 0;
                                            //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = 0;
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                            dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// ((depth & 0xFF00) >> 8);
                                        }
                                    }
                                }
                                else
                                {
                                    if (pHL2ResearchMode->IsCapturingBinaryDepth())
                                    {
                                        //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 0] = 0;
                                        //dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 2 * j + 1] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 0] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 1] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 2] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 3] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 4] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 5] = 0;
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 6] = 0xFF;// ((depth & 0xFF00) >> 8);
                                        dataInBytesDepth[bufferLayoutDepth.StartIndex + bufferLayoutDepth.Stride * i + 8 * j + 7] = 0xFF;// ((depth & 0xFF00) >> 8);
                                    }
                                }
                            }
                        }
                    }
                }

                if (pHL2ResearchMode->IsCapturingBinaryDepth())
                {
                    wchar_t fName[128];
                    swprintf(fName, 128, L"%s_%s_localPC.bmp", m_datetime.c_str(), m_ms);// depthTimestampString);
                    //depthImage = winrt::Windows::Graphics::Imaging::SoftwareBitmap::Convert(depthImage, winrt::Windows::Graphics::Imaging::BitmapPixelFormat::Rgba16);
                    //std::wstring pcName = fullName + L"\\" + m_datetime + L"_" + m_ms + L"_color.png";
                    winrt::Windows::Storage::StorageFolder storageFolder = winrt::Windows::Storage::ApplicationData::Current().LocalFolder();
                    pHL2ResearchMode->_lastBinaryDepthName = storageFolder.Path();
                    pHL2ResearchMode->_lastBinaryDepthName = pHL2ResearchMode->_lastBinaryDepthName + hstring(L"\\") + hstring(fName);

                    CreateLocalFile(fName, localPCImage, true);
                    //pHL2ResearchMode->_frameCount++;
                }
            }

            // save point cloud
            if (!pHL2ResearchMode->m_pointCloud)
            {
                OutputDebugString(L"Create Space for point cloud...\n");
                pHL2ResearchMode->m_pointCloud = new float[outBufferCount * 6];
            }

            memcpy(pHL2ResearchMode->m_pointCloud, pointCloud.data(), pointCloud.size() * sizeof(float));
            pHL2ResearchMode->m_pointcloudLength = pointCloud.size();
                
            if (!pHL2ResearchMode->m_localDepth)
            {
                OutputDebugString(L"Create Space for local depth...\n");
                pHL2ResearchMode->m_localDepth = new float[outBufferCount * 4];
            }

            if (!pHL2ResearchMode->m_longAbImage)
            {
                OutputDebugString(L"Create Space for long AbImage...\n");
                pHL2ResearchMode->m_longAbImage = new UINT16[outBufferCount];
            }
            memcpy(pHL2ResearchMode->m_longAbImage, pActiveBrightness, outBufferCount * sizeof(UINT16));

            // save raw depth map
            if (!pHL2ResearchMode->m_longDepthMap)
            {
                OutputDebugString(L"Create Space for depth map...\n");
                pHL2ResearchMode->m_longDepthMap = new UINT16[outBufferCount];
            }
            memcpy(pHL2ResearchMode->m_longDepthMap, pDepth, outBufferCount * sizeof(UINT16));

            if (!pHL2ResearchMode->m_depthMapFiltered)
            {
                OutputDebugString(L"Create Space for depth map...\n");
                pHL2ResearchMode->m_depthMapFiltered = new UINT16[outBufferCount];
            }

            memcpy(pHL2ResearchMode->m_depthMapFiltered, pDepthTextureFiltered.get(), outBufferCount * sizeof(UINT16));

            // save pre-processed depth map texture (for visualization)
            if (!pHL2ResearchMode->m_longDepthMapTexture)
            {
                OutputDebugString(L"Create Space for depth map texture...\n");
                pHL2ResearchMode->m_longDepthMapTexture = new UINT8[outBufferCount];
            }
            memcpy(pHL2ResearchMode->m_longDepthMapTexture, pDepthTexture.get(), outBufferCount * sizeof(UINT8));

            //OutputDebugString(L"Setting long depth updated to TRUE..\n");
            pHL2ResearchMode->m_longDepthMapTextureUpdated = true;
            pHL2ResearchMode->m_pointCloudUpdated = true;

            pHL2ResearchMode->mu.unlock();

            //might not need to assign to these textures anymore as we're not using them...
            pDepthTexture.reset();

            pDepthTextureFiltered.reset();

            // release space
            if (pDepthFrame) {
                pDepthFrame->Release();
            }
            if (pDepthSensorFrame)
            {
                pDepthSensorFrame->Release();
            }

            fc++;
        }
        
        pHL2ResearchMode->m_longDepthSensor->CloseStream();
        pHL2ResearchMode->m_longDepthSensor->Release();
        pHL2ResearchMode->m_longDepthSensor = nullptr;
        OutputDebugString(L"Closing Stream...\n");
    }

    void HL2ResearchMode::StartSpatialCamerasFrontLoop()
    {
        if (m_refFrame == nullptr)
        {
            m_refFrame = m_locator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem();
        }

        m_pSpatialCamerasFrontUpdateThread = new std::thread(HL2ResearchMode::SpatialCamerasFrontLoop, this);
    }

    void HL2ResearchMode::SpatialCamerasFrontLoop(HL2ResearchMode* pHL2ResearchMode)
    {
        // prevent starting loop for multiple times
        if (!pHL2ResearchMode->m_spatialCamerasFrontLoopStarted)
        {
            pHL2ResearchMode->m_spatialCamerasFrontLoopStarted = true;
        }
        else {
            return;
        }

        pHL2ResearchMode->m_LFSensor->OpenStream();
        pHL2ResearchMode->m_RFSensor->OpenStream();
        pHL2ResearchMode->m_LFSensorSide->OpenStream();
        pHL2ResearchMode->m_RFSensorSide->OpenStream();

        try
        {
            while (pHL2ResearchMode->m_spatialCamerasFrontLoopStarted)
            {
                IResearchModeSensorFrame* pLFCameraFrame = nullptr;
                IResearchModeSensorFrame* pRFCameraFrame = nullptr;
                ResearchModeSensorResolution LFResolution;
                ResearchModeSensorResolution RFResolution;

                IResearchModeSensorFrame* pLRCameraFrame = nullptr;
                IResearchModeSensorFrame* pRRCameraFrame = nullptr;
                ResearchModeSensorResolution LRResolution;
                ResearchModeSensorResolution RRResolution;

                pHL2ResearchMode->m_LFSensor->GetNextBuffer(&pLFCameraFrame);
				pHL2ResearchMode->m_RFSensor->GetNextBuffer(&pRFCameraFrame);
                pHL2ResearchMode->m_LFSensorSide->GetNextBuffer(&pLRCameraFrame);
                pHL2ResearchMode->m_RFSensorSide->GetNextBuffer(&pRRCameraFrame);

                // process sensor frame
                pLFCameraFrame->GetResolution(&LFResolution);
                pHL2ResearchMode->m_LFResolution = LFResolution;
                pRFCameraFrame->GetResolution(&RFResolution);
                pHL2ResearchMode->m_RFResolution = RFResolution;
                
                pLRCameraFrame->GetResolution(&LRResolution);
                pHL2ResearchMode->m_LRResolution = LRResolution;
                pRRCameraFrame->GetResolution(&RRResolution);
                pHL2ResearchMode->m_RRResolution = RRResolution;

                IResearchModeSensorVLCFrame* pLFFrame = nullptr;
                winrt::check_hresult(pLFCameraFrame->QueryInterface(IID_PPV_ARGS(&pLFFrame)));
                IResearchModeSensorVLCFrame* pRFFrame = nullptr;
                winrt::check_hresult(pRFCameraFrame->QueryInterface(IID_PPV_ARGS(&pRFFrame)));

                IResearchModeSensorVLCFrame* pLRFrame = nullptr;
                winrt::check_hresult(pLRCameraFrame->QueryInterface(IID_PPV_ARGS(&pLRFrame)));
                IResearchModeSensorVLCFrame* pRRFrame = nullptr;
                winrt::check_hresult(pRRCameraFrame->QueryInterface(IID_PPV_ARGS(&pRRFrame)));

                size_t LFOutBufferCount = 0;
                const BYTE *pLFImage = nullptr;
                pLFFrame->GetBuffer(&pLFImage, &LFOutBufferCount);
                pHL2ResearchMode->m_LFbufferSize = LFOutBufferCount;

				size_t RFOutBufferCount = 0;
				const BYTE *pRFImage = nullptr;
				pRFFrame->GetBuffer(&pRFImage, &RFOutBufferCount);
				pHL2ResearchMode->m_RFbufferSize = RFOutBufferCount;

                size_t LROutBufferCount = 0;
                const BYTE* pLRImage = nullptr;
                pLRFrame->GetBuffer(&pLRImage, &LROutBufferCount);
                pHL2ResearchMode->m_LRbufferSize = LROutBufferCount;

                size_t RROutBufferCount = 0;
                const BYTE* pRRImage = nullptr;
                pRRFrame->GetBuffer(&pRRImage, &RROutBufferCount);
                pHL2ResearchMode->m_RRbufferSize = RROutBufferCount;

                // get tracking transform
                ResearchModeSensorTimestamp timestamp;
                pLFCameraFrame->GetTimeStamp(&timestamp);

                auto ts = PerceptionTimestampHelper::FromSystemRelativeTargetTime(HundredsOfNanoseconds(checkAndConvertUnsigned(timestamp.HostTicks)));
                auto transToWorld = pHL2ResearchMode->m_locator.TryLocateAtTimestamp(ts, pHL2ResearchMode->m_refFrame);
                if (transToWorld == nullptr)
                {
                    continue;
                }
                auto rot = transToWorld.Orientation();
                /*{
                    std::stringstream ss;
                    ss << rot.x << "," << rot.y << "," << rot.z << "," << rot.w << "\n";
                    std::string msg = ss.str();
                    std::wstring widemsg = std::wstring(msg.begin(), msg.end());
                    OutputDebugString(widemsg.c_str());
                }*/
                auto quatInDx = XMFLOAT4(rot.x, rot.y, rot.z, rot.w);
                auto rotMat = XMMatrixRotationQuaternion(XMLoadFloat4(&quatInDx));
                auto pos = transToWorld.Position();
                auto posMat = XMMatrixTranslation(pos.x, pos.y, pos.z);
                auto LfToWorld = pHL2ResearchMode->m_LFCameraPoseInvMatrix * rotMat * posMat;
				auto RfToWorld = pHL2ResearchMode->m_RFCameraPoseInvMatrix * rotMat * posMat;

                auto LrToWorld = pHL2ResearchMode->m_LFCameraPoseInvMatrixSide * rotMat * posMat;
                auto RrToWorld = pHL2ResearchMode->m_RFCameraPoseInvMatrixSide * rotMat * posMat;


                // save data
                {
                    std::lock_guard<std::mutex> l(pHL2ResearchMode->mu);

					// save LF and RF images
					if (!pHL2ResearchMode->m_LFImage)
					{
						OutputDebugString(L"Create Space for Left Front Image...\n");
						pHL2ResearchMode->m_LFImage = new UINT8[LFOutBufferCount];
					}
					memcpy(pHL2ResearchMode->m_LFImage, pLFImage, LFOutBufferCount * sizeof(UINT8));

					if (!pHL2ResearchMode->m_RFImage)
					{
						OutputDebugString(L"Create Space for Right Front Image...\n");
						pHL2ResearchMode->m_RFImage = new UINT8[RFOutBufferCount];
					}
					memcpy(pHL2ResearchMode->m_RFImage, pRFImage, RFOutBufferCount * sizeof(UINT8));

                    if (!pHL2ResearchMode->m_LRImage)
                    {
                        OutputDebugString(L"Create Space for Left Side Image...\n");
                        pHL2ResearchMode->m_LRImage = new UINT8[LROutBufferCount];
                    }
                    memcpy(pHL2ResearchMode->m_LRImage, pLRImage, LROutBufferCount * sizeof(UINT8));
                   
                    if (!pHL2ResearchMode->m_RRImage)
                    {
                        OutputDebugString(L"Create Space for Right Side Image...\n");
                        pHL2ResearchMode->m_RRImage = new UINT8[RROutBufferCount];
                    }
                    memcpy(pHL2ResearchMode->m_RRImage, pRRImage, RROutBufferCount * sizeof(UINT8));

                }
				pHL2ResearchMode->m_LFImageUpdated = true;
				pHL2ResearchMode->m_RFImageUpdated = true;
                pHL2ResearchMode->m_LRImageUpdated = true;
                pHL2ResearchMode->m_RRImageUpdated = true;

                // release space
				if (pLFFrame) pLFFrame->Release();
				if (pRFFrame) pRFFrame->Release();
                if (pLRFrame) pLRFrame->Release();
                if (pRRFrame) pRRFrame->Release();

				if (pLFCameraFrame) pLFCameraFrame->Release();
				if (pRFCameraFrame) pRFCameraFrame->Release();

                if (pLRCameraFrame) pLRCameraFrame->Release();
                if (pRRCameraFrame) pRRCameraFrame->Release();
            }
        }
        catch (...) {}
        pHL2ResearchMode->m_LFSensor->CloseStream();
        pHL2ResearchMode->m_LFSensor->Release();
        pHL2ResearchMode->m_LFSensor = nullptr;

		pHL2ResearchMode->m_RFSensor->CloseStream();
		pHL2ResearchMode->m_RFSensor->Release();
		pHL2ResearchMode->m_RFSensor = nullptr;

        pHL2ResearchMode->m_LFSensorSide->CloseStream();
        pHL2ResearchMode->m_LFSensorSide->Release();
        pHL2ResearchMode->m_LFSensorSide = nullptr;

        pHL2ResearchMode->m_RFSensorSide->CloseStream();
        pHL2ResearchMode->m_RFSensorSide->Release();
        pHL2ResearchMode->m_RFSensorSide = nullptr;
    }

    void HL2ResearchMode::CamAccessOnComplete(ResearchModeSensorConsent consent)
    {
        camAccessCheck = consent;
        SetEvent(camConsentGiven);
    }

    inline UINT16 HL2ResearchMode::GetCenterDepth() {return m_centerDepth;}

    inline int HL2ResearchMode::GetDepthBufferSize() { return m_depthBufferSize; }

    inline bool HL2ResearchMode::DepthMapTextureUpdated() { return m_depthMapTextureUpdated; }

    inline bool HL2ResearchMode::ShortAbImageTextureUpdated() { return m_shortAbImageTextureUpdated; }

    inline bool HL2ResearchMode::PointCloudUpdated() { return m_pointCloudUpdated; }

    inline int HL2ResearchMode::GetLongDepthBufferSize() { return m_longDepthBufferSize; }

    bool HL2ResearchMode::LongDepthMapTextureUpdated() 
    { 
        bool updated;
        mu.lock();
        updated = m_longDepthMapTextureUpdated; 
        mu.unlock();
        /*if (updated)
        {
            OutputDebugString(L"Long depth updated...\n");
        }
        else
        {
            OutputDebugString(L"Long depth not updated...\n");
        }*/
        return updated;
    }

	inline bool HL2ResearchMode::LFImageUpdated() { return m_LFImageUpdated; }

	inline bool HL2ResearchMode::RFImageUpdated() { return m_RFImageUpdated; }

    inline bool HL2ResearchMode::LRImageUpdated() { return m_LRImageUpdated; }

    inline bool HL2ResearchMode::RRImageUpdated() { return m_RRImageUpdated; }

    /*hstring HL2ResearchMode::PrintDepthResolution()
    {
        std::string res_c_ctr = std::to_string(m_depthResolution.Height) + "x" + std::to_string(m_depthResolution.Width) + "x" + std::to_string(m_depthResolution.BytesPerPixel);
        return winrt::to_hstring(res_c_ctr);
    }

    hstring HL2ResearchMode::PrintDepthExtrinsics()
    {
        std::stringstream ss;
        ss << "Extrinsics: \n" << MatrixToString(m_depthCameraPose);
        std::string msg = ss.str();
        std::wstring widemsg = std::wstring(msg.begin(), msg.end());
        OutputDebugString(widemsg.c_str());
        return winrt::to_hstring(msg);
    }

	hstring HL2ResearchMode::PrintLFResolution()
	{
		std::string res_c_ctr = std::to_string(m_LFResolution.Height) + "x" + std::to_string(m_LFResolution.Width) + "x" + std::to_string(m_LFResolution.BytesPerPixel);
		return winrt::to_hstring(res_c_ctr);
	}

	hstring HL2ResearchMode::PrintLFExtrinsics()
	{
		std::stringstream ss;
		ss << "Extrinsics: \n" << MatrixToString(m_LFCameraPose);
		std::string msg = ss.str();
		std::wstring widemsg = std::wstring(msg.begin(), msg.end());
		OutputDebugString(widemsg.c_str());
		return winrt::to_hstring(msg);
	}

	hstring HL2ResearchMode::PrintRFResolution()
	{
		std::string res_c_ctr = std::to_string(m_RFResolution.Height) + "x" + std::to_string(m_RFResolution.Width) + "x" + std::to_string(m_RFResolution.BytesPerPixel);
		return winrt::to_hstring(res_c_ctr);
	}

	hstring HL2ResearchMode::PrintRFExtrinsics()
	{
		std::stringstream ss;
		ss << "Extrinsics: \n" << MatrixToString(m_RFCameraPose);
		std::string msg = ss.str();
		std::wstring widemsg = std::wstring(msg.begin(), msg.end());
		OutputDebugString(widemsg.c_str());
		return winrt::to_hstring(msg);
	}*/

    std::string HL2ResearchMode::MatrixToString(DirectX::XMFLOAT4X4 mat)
    {
        std::stringstream ss;
        for (size_t i = 0; i < 4; i++)
        {
            for (size_t j = 0; j < 4; j++)
            {
                ss << mat(i, j) << ",";
            }
            ss << "\n";
        }
        return ss.str();
    }
    
    // Stop the sensor loop and release buffer space.
    // Sensor object should be released at the end of the loop function
    void HL2ResearchMode::StopAllSensorDevice()
    {
        m_depthSensorLoopStarted = false;
        m_PVLoopStarted = false;
        //m_pDepthUpdateThread->join();
        if (m_depthMap) 
        {
            delete[] m_depthMap;
            m_depthMap = nullptr;
        }

        if (m_depthMapFiltered)
        {
            delete[] m_depthMapFiltered;
            m_depthMapFiltered = nullptr;
        }

        if (_depthPts)
        {
            delete[] _depthPts;
            _depthPts = nullptr;
        }

        if (m_depthMapTexture) 
        {
            delete[] m_depthMapTexture;
            m_depthMapTexture = nullptr;
        }

        if (m_localDepth)
        {
            delete[] m_localDepth;
            m_localDepth = nullptr;
        }

        if (m_pointCloud) 
        {
            m_pointcloudLength = 0;
            delete[] m_pointCloud;
            m_pointCloud = nullptr;
        }

        m_longDepthSensorLoopStarted = false;

        if (m_longDepthMap)
        {
            delete[] m_longDepthMap;
            m_longDepthMap = nullptr;
        }

        if (m_longDepthMapTexture)
        {
            delete[] m_longDepthMapTexture;
            m_longDepthMapTexture = nullptr;
        }

        if (m_shortAbImage)
        {
            delete[] m_shortAbImage;
            m_shortAbImage = nullptr;
        }

        if (m_shortAbImageTexture)
        {
            delete[] m_shortAbImageTexture;
            m_shortAbImageTexture = nullptr;
        }

        if (m_longAbImage)
        {
            delete[] m_longAbImage;
            m_longAbImage = nullptr;
        }

        if (m_localPointCloud)
        {
            delete[] m_localPointCloud;
            m_localPointCloud = nullptr;
        }

        m_mediaFrameReader.FrameArrived(m_OnFrameArrivedRegistration);

        if (m_pSensorDevice != nullptr)
        {
            m_pSensorDevice->Release();
            m_pSensorDevice = nullptr;
        }

        if (m_pSensorDeviceConsent != nullptr)
        {
            m_pSensorDeviceConsent->Release();
            m_pSensorDeviceConsent = nullptr;
        }
    }

    com_array<uint8_t> HL2ResearchMode::GetPVColorBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (!m_pixelBufferData)
        {
            return com_array<UINT8>();
        }
        com_array<UINT8> tempBuffer = com_array<UINT8>(std::move_iterator(m_pixelBufferData), std::move_iterator(m_pixelBufferData + m_colorBufferSize)); //m_pixelBufferData, m_pixelBufferData + m_colorBufferSize); //

        //m_depthMapTextureUpdated = false;
        return tempBuffer;
    }

    com_array<uint16_t> HL2ResearchMode::GetDepthMapBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (!m_depthMap)
        {
            return com_array<uint16_t>();
        }
        com_array<UINT16> tempBuffer = com_array<UINT16>(m_depthMap, m_depthMap + m_depthBufferSize);
        m_depthMapTextureUpdated = false;
        return tempBuffer;
    }

    com_array<uint16_t> HL2ResearchMode::GetShortAbImageBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (!m_shortAbImage)
        {
            return com_array<uint16_t>();
        }
        com_array<UINT16> tempBuffer = com_array<UINT16>(m_shortAbImage, m_shortAbImage + m_depthBufferSize);

        return tempBuffer;
    }

    // Get depth map texture buffer. (For visualization purpose)
    com_array<uint8_t> HL2ResearchMode::GetDepthMapTextureBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (!m_depthMapTexture) 
        {
            return com_array<UINT8>();
        }
        com_array<UINT8> tempBuffer = com_array<UINT8>(std::move_iterator(m_depthMapTexture), std::move_iterator(m_depthMapTexture + m_depthBufferSize));

        m_depthMapTextureUpdated = false;
        return tempBuffer;
    }

    // Get depth map texture buffer. (For visualization purpose)
    com_array<uint8_t> HL2ResearchMode::GetShortAbImageTextureBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (!m_shortAbImageTexture)
        {
            return com_array<UINT8>();
        }
        com_array<UINT8> tempBuffer = com_array<UINT8>(std::move_iterator(m_shortAbImageTexture), std::move_iterator(m_shortAbImageTexture + m_depthBufferSize));

        m_shortAbImageTextureUpdated = false;
        return tempBuffer;
    }

    com_array<uint16_t> HL2ResearchMode::GetLongDepthMapBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (!m_longDepthMap)
        {
            return com_array<uint16_t>();
        }
        com_array<UINT16> tempBuffer = com_array<UINT16>(m_longDepthMap, m_longDepthMap + m_longDepthBufferSize);
        //m_longDepthMapTextureUpdated = false;
        return tempBuffer;
    }
	
    com_array<uint16_t> HL2ResearchMode::GetDepthMapBufferFiltered()
    {
        std::lock_guard<std::mutex> l(mu);
        if (!m_depthMapFiltered)
        {
            return com_array<uint16_t>();
        }
        com_array<UINT16> tempBuffer = com_array<UINT16>(m_depthMapFiltered, m_depthMapFiltered + m_longDepthBufferSize);
        //m_longDepthMapTextureUpdated = false;
        return tempBuffer;
    }
	
    com_array<uint8_t> HL2ResearchMode::GetLongDepthMapTextureBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (!m_longDepthMapTexture)
        {
            return com_array<UINT8>();
        }
        com_array<UINT8> tempBuffer = com_array<UINT8>(std::move_iterator(m_longDepthMapTexture), std::move_iterator(m_longDepthMapTexture + m_longDepthBufferSize));

        //m_longDepthMapTextureUpdated = false;
        return tempBuffer;
    }

	com_array<uint8_t> HL2ResearchMode::GetLFCameraBuffer()
	{
		std::lock_guard<std::mutex> l(mu);
		if (!m_LFImage)
		{
			return com_array<UINT8>();
		}
		com_array<UINT8> tempBuffer = com_array<UINT8>(std::move_iterator(m_LFImage), std::move_iterator(m_LFImage + m_LFbufferSize));

		m_LFImageUpdated = false;
		return tempBuffer;
	}

    com_array<uint8_t> HL2ResearchMode::GetLRCameraBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (!m_LRImage)
        {
            return com_array<UINT8>();
        }
        com_array<UINT8> tempBuffer = com_array<UINT8>(std::move_iterator(m_LRImage), std::move_iterator(m_LRImage + m_LRbufferSize));

        m_LRImageUpdated = false;
        return tempBuffer;
    }

	com_array<uint8_t> HL2ResearchMode::GetRFCameraBuffer()
	{
		std::lock_guard<std::mutex> l(mu);
		if (!m_RFImage)
		{
			return com_array<UINT8>();
		}
		com_array<UINT8> tempBuffer = com_array<UINT8>(std::move_iterator(m_RFImage), std::move_iterator(m_RFImage + m_RFbufferSize));

		m_RFImageUpdated = false;
		return tempBuffer;
	}

    com_array<uint8_t> HL2ResearchMode::GetRRCameraBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (!m_RRImage)
        {
            return com_array<UINT8>();
        }
        com_array<UINT8> tempBuffer = com_array<UINT8>(std::move_iterator(m_RRImage), std::move_iterator(m_RRImage + m_RRbufferSize));

        m_RRImageUpdated = false;
        return tempBuffer;
    }

    // Get the buffer for point cloud in the form of float array.
    // There will be 3n elements in the array where the 3i, 3i+1, 3i+2 element correspond to x, y, z component of the i'th point. (i->[0,n-1])
    com_array<float> HL2ResearchMode::GetPointCloudBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (m_pointcloudLength == 0)
        {
            return com_array<float>();
        }
        com_array<float> tempBuffer = com_array<float>(std::move_iterator(m_pointCloud), std::move_iterator(m_pointCloud + m_pointcloudLength));
        m_pointCloudUpdated = false;
        return tempBuffer;
    }

    com_array<float> HL2ResearchMode::GetLocalDepthBuffer()
    {
        std::lock_guard<std::mutex> l(mu);
        if (m_localDepthLength == 0)
        {
            return com_array<float>();
        }
        com_array<float> tempBuffer = com_array<float>(std::move_iterator(m_localDepth), std::move_iterator(m_localDepth + m_localDepthLength));
        return tempBuffer;
    }

    // Get the 3D point (float[3]) of center point in depth map. Can be used to render depth cursor.
    com_array<float> HL2ResearchMode::GetCenterPoint()
    {
        std::lock_guard<std::mutex> l(mu);
        com_array<float> centerPoint = com_array<float>(std::move_iterator(m_centerPoint), std::move_iterator(m_centerPoint + 3));

        return centerPoint;
    }

    com_array<float> HL2ResearchMode::GetDepthSensorPosition()
    {
        std::lock_guard<std::mutex> l(mu);
        com_array<float> depthSensorPos = com_array<float>(std::move_iterator(m_depthSensorPosition), std::move_iterator(m_depthSensorPosition + 3));

        return depthSensorPos;
    }

    com_array<float> HL2ResearchMode::GetDepthToWorld()
    {
        std::lock_guard<std::mutex> l(mu);
        com_array<float> depthToWorld = com_array<float>(std::move_iterator(m_depthToWorld), std::move_iterator(m_depthToWorld + 16));

        return depthToWorld;
    }

    com_array<float> HL2ResearchMode::GetCurrRotation()
    {
        std::lock_guard<std::mutex> l(mu);
        com_array<float> depthToWorld = com_array<float>(std::move_iterator(m_currRotation), std::move_iterator(m_currRotation + 16));

        return depthToWorld;
    }

    com_array<float> HL2ResearchMode::GetCurrPosition()
    {
        std::lock_guard<std::mutex> l(mu);
        com_array<float> depthToWorld = com_array<float>(std::move_iterator(m_currPosition), std::move_iterator(m_currPosition + 16));

        return depthToWorld;
    }

    com_array<float> HL2ResearchMode::GetPVMatrix()
    {
        std::lock_guard<std::mutex> l(mu);
        com_array<float> colorToWorld = com_array<float>(std::move_iterator(m_PVToWorld), std::move_iterator(m_PVToWorld + 16));

        return colorToWorld;
    }

    // Set the reference coordinate system. Need to be set before the sensor loop starts; otherwise, default coordinate will be used.
    void HL2ResearchMode::SetReferenceCoordinateSystem(guid g)
    {
        SpatialCoordinateSystem refCoord = SpatialGraphInteropPreview::CreateCoordinateSystemForNode(g);
        m_refFrame = refCoord;
    }

    void HL2ResearchMode::SetPointCloudRoiInSpace(float centerX, float centerY, float centerZ, float boundX, float boundY, float boundZ)
    {
        std::lock_guard<std::mutex> l(mu);

        m_useRoiFilter = true;
        m_roiCenter[0] = centerX;
        m_roiCenter[1] = centerY;
        m_roiCenter[2] = -centerZ;

        m_roiBound[0] = boundX;
        m_roiBound[1] = boundY;
        m_roiBound[2] = boundZ;
    }

    void HL2ResearchMode::SetPointCloudDepthOffset(uint16_t offset)
    {
        m_depthOffset = offset;
    }

    void HL2ResearchMode::SetQRCodeDetected()
    {
        m_bIsQRCodeDetected = true;
        OutputDebugString(L"Found QR Code...\n");
    }

    void HL2ResearchMode::SetQRTransform(float f00, float f01, float f02, float f03, float f10, float f11, float f12, float f13, float f20, float f21, float f22, float f23, float f30, float f31, float f32, float f33)
    {
        m_QRTransform[0] = f00;
        m_QRTransform[1] = f01;
        m_QRTransform[2] = f02;
        m_QRTransform[3] = f03;
        m_QRTransform[4] = f10;
        m_QRTransform[5] = f11;
        m_QRTransform[6] = f12;
        m_QRTransform[7] = f13;
        m_QRTransform[8] = f20;
        m_QRTransform[9] = f21;
        m_QRTransform[10] = f22;
        m_QRTransform[11] = f23;
        m_QRTransform[12] = f30;
        m_QRTransform[13] = f31;
        m_QRTransform[14] = f32;
        m_QRTransform[15] = f33;

        m_QR._11 = m_QRTransform[0];
        m_QR._12 = m_QRTransform[1];
        m_QR._13 = m_QRTransform[2];
        m_QR._14 = m_QRTransform[3];
        m_QR._21 = m_QRTransform[4];
        m_QR._22 = m_QRTransform[5];
        m_QR._23 = m_QRTransform[6];
        m_QR._24 = m_QRTransform[7];
        m_QR._31 = m_QRTransform[8];
        m_QR._32 = m_QRTransform[9];
        m_QR._33 = m_QRTransform[10];
        m_QR._34 = m_QRTransform[11];
        m_QR._41 = m_QRTransform[12];
        m_QR._42 = m_QRTransform[13];
        m_QR._43 = m_QRTransform[14];
        m_QR._44 = m_QRTransform[15];

        m_QRMatrix = XMLoadFloat4x4(&m_QR);

        
        //SpatialAnchor s = SpatialAnchor::TryCreateRelativeTo(m_refFrame);
        //s.CoordinateSystem().TryGetTransformTo()
        /*float4x4 anchorSpaceToCurrentCoordinateSystem;
        SpatialCoordinateSystem^ anchorSpace = someAnchor->CoordinateSystem;
        const auto tryTransform = anchorSpace->TryGetTransformTo(currentCoordinateSystem);
        if (tryTransform != nullptr)
        {
            anchorSpaceToCurrentCoordinateSystem = tryTransform->Value;
        }*/
    }

    long long HL2ResearchMode::checkAndConvertUnsigned(UINT64 val)
    {
        //assert(val <= kMaxLongLong);
        return static_cast<long long>(val);
    }

}
