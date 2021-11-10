# Nist 
##  *HoloLens2 Research Mode Unity Plugin*

## Required Tech

- [Hololens 2] (Notice that Nist is not yet compatible with Hololens 1).
- [Unity] 
- [Visual Studio Code 2019/2022] 

## Pre-Installation 
1. Pair Hololens 2 with your PC (pair your device using the USB method). [Click here to see how](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-the-windows-device-portal#connecting-over-usb)
2. Switch on the Research Mode in Hololens. [Click here to see how](https://github.com/microsoft/HoloLens2ForCV/blob/main/Docs/ECCV2020-Tutorial/ECCV2020-ResearchMode-Api.pdf)
3. Make sure that all the necessary packages are install on Studio Code 2019/2021. [Click here to see the list of packages](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/install-the-tools#installation-checklist)



## Installation

1. Clone this repo to your local PC. 
```sh
git clone https://github.com/widVE/NIST.git
```

## Steps
1. Open up **NIST/ResearchModePlugin/HL2Unityplugin.sln** in Visual Studio Code 2019/2022. 
2. **Build** this project for **Release and ARM 64** in Visual Studio 2019/2022. 
3. Copy the DLL files that are generated in the **NIST/ResearchModePlugin/ARM64/Release/HL2UnityPlugin** folder to **NIST/Assets/Plugins/WSA/ARM64** (replace the DLL file if there already exist one).
4. Open the NIST project in **Unity**.
5. Switch the to **Window Universal Platform** in **File -> Building Settings** in Unity, change the **Target Device** to Hololens, change the **Architecture to ARM64**. Note: Don’t forget to click Add Open Scenes. 
6. Make any changes you want to the Unity project, and then do a **build for Windows Universal Platform** - I typically build it in a folder I call NIST/NISTBuild
7. Attach the Hololens 2 via **USB** and enter the pin.
8. Open the built file (in our example is NIST/NISTBuild/NIST_AR.sln) in Visual Studio 2019/2022 and build for **ARM64 Release**. 
9. Make sure the Text next to the Green Arrow in Visual Studio says **"Device" and not "Remote Machine"**.
10. Click the **Green Arrow** Next to Device.  This will deploy the app to the Hololens 2 and start the debugger.
