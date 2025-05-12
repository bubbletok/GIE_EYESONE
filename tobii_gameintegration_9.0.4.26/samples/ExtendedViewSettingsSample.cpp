#include "tobii_gameintegration.h"
#include <iostream>
#include "windows.h"
#include <thread>
#include <vector>
#include <algorithm>
#include <functional>
#include <string>
#include <iomanip>

using namespace TobiiGameIntegration;

HWND GetConsoleHwnd(void); // See SampleHelpFunctions.cpp

struct SettingItem
{
    std::string m_name;
    bool m_valueChanged = false;
        
    SettingItem(std::string name) : m_name{ name } { };

    virtual float GetValue() = 0;
    virtual void ChangeValue(bool increase) = 0;
    virtual void Draw(bool isSelected) const = 0;
};

// This class represents a hypothetical in-game slider that the user can control.
struct Slider : public SettingItem
{
    const float m_minValue;
    const float m_maxValue;
    float& m_value;

    Slider(std::string name, float min, float max, float& current) 
        : SettingItem(name), m_minValue{min}, m_maxValue{ max }, m_value{ current }
    { }

    virtual float GetValue() override
    {
        return m_value;
    }

    void ChangeValue(bool increase) override
    {
        float stepSize = (m_maxValue - m_minValue) / 10.f;
        float newValue = increase ? m_value + stepSize : m_value - stepSize;
        newValue = std::min<float>(m_maxValue, std::max<float>(m_minValue, newValue));
        if (newValue != m_value)
        {
            m_valueChanged = true;
            m_value = newValue;
        }
    }

    void Draw(bool isSelected) const override
    {
        std::cout << (isSelected ? "->" : "  ") << "[" << "] " << m_name << " : " << m_value << "                       " << std::endl;
    }
};

struct Switch : public SettingItem
{
    bool m_value = false;

    Switch(std::string name, bool currentValue)
        : SettingItem(name), m_value{ currentValue }
    { }

    virtual float GetValue() override
    {
        return m_value ? 1.0f : 0.0f;
    }

    void ChangeValue(bool increase) override
    {
        m_value = !m_value;
        m_valueChanged = true;
    }

    void Draw(bool isSelected) const override
    {
        std::cout << (isSelected ? "->" : "  ") << "[" << (m_value ? " #" : "# ") << "] " << m_name << " : " << (m_value ? "True " : "False") << "                       " << std::endl;
    }
};

// Forward declaration: This function is responsible for printing a list of settings and also querying user input to select a setting
void PrintExtendedViewSettingsControl(std::vector<SettingItem*>& allSettings, bool forceDraw = false);

void ExtendedViewSettingsSample()
{
    ExtendedViewSettings s;

    float gazePlusHeadPitchLimitDegrees = 70.0f;
    float eyeToHeadLimitsRatio = 1.0f;
    float headCenterStabilisation = 0.f;
    float headRotationSensitivity = 1.0f;

    std::vector<Slider> settingsSliders =
    {
        // These are examples of detailed settings sliders, which are bound here directly to single Setting members of the ExtendedViewSettings struct:
        { "GazeHeadMix.GazePitchUpLimitDegrees",
            s.GazeHeadMix.GazePitchUpLimitDegrees.Metadata.MinMaxRange.Min,
            s.GazeHeadMix.GazePitchUpLimitDegrees.Metadata.MinMaxRange.Max,
            s.GazeHeadMix.GazePitchUpLimitDegrees },
        { "HeadTracking.HeadPitchUpDegrees.Limit",
            s.HeadTracking.PitchUpDegrees.Limit.Metadata.MinMaxRange.Min,
            s.HeadTracking.PitchUpDegrees.Limit.Metadata.MinMaxRange.Max,
            s.HeadTracking.PitchUpDegrees.Limit },
        { "HeadTracking.PitchUpDegrees.SensitivityScaling",
            s.HeadTracking.PitchUpDegrees.SensitivityScaling.Metadata.MinMaxRange.Min,
            s.HeadTracking.PitchUpDegrees.SensitivityScaling.Metadata.MinMaxRange.Max,
            s.HeadTracking.PitchUpDegrees.SensitivityScaling },
        { "HeadTracking.PitchUpDegrees.DeadZoneNorm",
            s.HeadTracking.PitchUpDegrees.DeadZoneNorm.Metadata.MinMaxRange.Min,
            s.HeadTracking.PitchUpDegrees.DeadZoneNorm.Metadata.MinMaxRange.Max,
            s.HeadTracking.PitchUpDegrees.DeadZoneNorm },
        { "HeadTracking.PitchUpDegrees.SCurveStrengthNorm",
            s.HeadTracking.PitchUpDegrees.SCurveStrengthNorm.Metadata.MinMaxRange.Min,
            s.HeadTracking.PitchUpDegrees.SCurveStrengthNorm.Metadata.MinMaxRange.Max,
            s.HeadTracking.PitchUpDegrees.SCurveStrengthNorm },

        { "GazeHeadMix.GazePitchDownLimitDegrees",
            s.GazeHeadMix.GazePitchDownLimitDegrees.Metadata.MinMaxRange.Min,
            s.GazeHeadMix.GazePitchDownLimitDegrees.Metadata.MinMaxRange.Max,
            s.GazeHeadMix.GazePitchDownLimitDegrees },
        { "HeadTracking.PitchDownDegrees.Limit",
            s.HeadTracking.PitchDownDegrees.Limit.Metadata.MinMaxRange.Min,
            s.HeadTracking.PitchDownDegrees.Limit.Metadata.MinMaxRange.Max,
            s.HeadTracking.PitchDownDegrees.Limit },
        { "HeadTracking.PitchDownDegrees.SensitivityScaling",
            s.HeadTracking.PitchDownDegrees.SensitivityScaling.Metadata.MinMaxRange.Min,
            s.HeadTracking.PitchDownDegrees.SensitivityScaling.Metadata.MinMaxRange.Max,
            s.HeadTracking.PitchDownDegrees.SensitivityScaling },
        { "HeadTracking.PitchDownDegrees.DeadZoneNorm",
            s.HeadTracking.PitchDownDegrees.DeadZoneNorm.Metadata.MinMaxRange.Min,
            s.HeadTracking.PitchDownDegrees.DeadZoneNorm.Metadata.MinMaxRange.Max,
            s.HeadTracking.PitchDownDegrees.DeadZoneNorm },
        { "HeadTracking.PitchDownDegrees.SCurveStrengthNorm",
            s.HeadTracking.PitchDownDegrees.SCurveStrengthNorm.Metadata.MinMaxRange.Min,
            s.HeadTracking.PitchDownDegrees.SCurveStrengthNorm.Metadata.MinMaxRange.Max,
            s.HeadTracking.PitchDownDegrees.SCurveStrengthNorm },

            // These are examples of compund settings sliders, which are bound here to local float variables that will be used to set multiple members of the ExtendedViewSettings struct via helper member-functions
            { "GazePlusHeadPitchLimitDegrees",
                0.0f,
                90.0f,
                gazePlusHeadPitchLimitDegrees },
            { "EyeToHeadLimitsRatio",
                0.0f,
                1.0f,
                eyeToHeadLimitsRatio },
            { "HeadCenterStabilisation",
                0.0f,
                1.0f,
                headCenterStabilisation },
            { "HeadRotationSensitivity",
                0.0f,
                5.0f,
                headRotationSensitivity }
    };

    std::vector<Switch> settingsSwitches = {
        { "RelativeHeadPositionEnabled", s.HeadTracking.RelativeHeadPositionEnabled },
        { "RotateAxisSettingsWithHead", s.HeadTracking.RotateAxisSettingsWithHead },
        { "CameraBoostEnabled", s.CameraBoost.Enabled },
        { "GazeHeadMixEnabled", s.GazeHeadMix.Enabled }

    };
    
    std::vector <SettingItem*> settings;

    for (auto& slider : settingsSliders)
        settings.push_back(&slider);

    for (auto& settingSwitch : settingsSwitches)
        settings.push_back(&settingSwitch);

    ITobiiGameIntegrationApi* api = GetApi("Extended View Settings Sample");
    IExtendedView* extendedView = api->GetFeatures()->GetExtendedView();

    api->GetTrackerController()->TrackWindow(GetConsoleHwnd());

    system("cls");
    std::cout << std::fixed << std::setprecision(3);
    PrintExtendedViewSettingsControl(settings, true); // true = force printing of settings even though no input changed

    while ((GetAsyncKeyState(VK_ESCAPE) & 0x8000) == 0)
    {
        api->Update();
        extendedView->GetSettings(s);

        PrintExtendedViewSettingsControl(settings);

        bool anySettingChanged = false;
        for (auto& setting : settings)
        {
            if (setting->m_valueChanged)
            {
                setting->m_valueChanged = false;
                anySettingChanged = true;

                // We handle changes to the compound settings Sliders by calling helper-functions which set multiple ExtendedViewSettings members
                // Note that these helpers-functions overwrite some of the individual Setting members which are used in the detailed settings above for illustrative purposes
                if (setting->m_name == "GazePlusHeadPitchLimitDegrees")
                {
                    GazeHeadMixHelpFunctions::SetCameraMaxAnglePitchUp(s.GazeHeadMix, s.HeadTracking, setting->GetValue());
                    GazeHeadMixHelpFunctions::SetCameraMaxAnglePitchDown(s.GazeHeadMix, s.HeadTracking, -setting->GetValue()); // Pitch down is negative
                }
                else if (setting->m_name == "EyeToHeadLimitsRatio")
                {
                    GazeHeadMixHelpFunctions::SetEyeHeadTrackingRatio(s.GazeHeadMix, s.HeadTracking, setting->GetValue());
                }
                else if (setting->m_name == "HeadCenterStabilisation")
                {
                    HeadTrackingHelpFunctions::SetCenterStabilization(s.HeadTracking, setting->GetValue());
                }
                else if (setting->m_name == "HeadRotationSensitivity")
                {
                    HeadTrackingHelpFunctions::SetHeadAllRotationAxisSettingsSensitivity(s.HeadTracking, setting->GetValue());
                }
                if (setting->m_name == "RelativeHeadPositionEnabled")
                {
                    // RelativeHeadPosition is a flavor of extended view in which head position is applied relative to the current head rotation.
                    // When enabled, turning head to the right 90° and then moving forward would result in movement "forward" in relation to the camera.
                    // When disabled, turning head to the right 90° and then moving forward would result in movement "left" in relation to the camera.
                    s.HeadTracking.RelativeHeadPositionEnabled = setting->GetValue() == 1.0f;
                }
                else if (setting->m_name == "RotateAxisSettingsWithHead")
                {
                    // Enables the rotation of AxisSettings together with Head. This setting relates to RelativeHeadPosition and is used only when RelativeHeadPosition is enabled.
                    // When disabled, the initial AxisSettings are respected. If you restrict head movement forward-backward to 1mm and left-right to 200mm,
                    // and then turn your head 90° to the right - you will have 1mm restriction on left-right head movement and 200mm forward-backward.
                    // When enabled, the Axis Settings are applied in relation to rotated head pose. If you restrict head movement forward-backward to 1mm and left-right to 200mm,
                    // and then turn your head 90° to the right - you will have 200mm restriction on left-right head movement and 1mm forward-backward.
                    s.HeadTracking.RotateAxisSettingsWithHead = setting->GetValue() == 1.0f;
                }
                else if (setting->m_name == "CameraBoostEnabled")
                {
                    s.CameraBoost.Enabled = setting->GetValue() == 1.0f;
                }
                else if (setting->m_name == "GazeHeadMixEnabled")
                {
                    s.GazeHeadMix.Enabled = setting->GetValue() == 1.0f;
                }
            }
        }

        if (anySettingChanged)
        {
            extendedView->UpdateSettings(s);
            PrintExtendedViewSettingsControl(settings, true);
        }

        const Transformation extendedViewTransformation = extendedView->GetTransformation();
        std::cout << "Extended View (deg): [ Yaw: " << extendedViewTransformation.Rotation.YawDegrees << " , Pitch: " << extendedViewTransformation.Rotation.PitchDegrees << " ]" << "               \r";

        Sleep(1000 / 60);
    }

    api->Shutdown();
}


void PrintExtendedViewSettingsControl(std::vector<SettingItem*>& allSettings, bool forceDraw /* = false */)
{
    const SHORT onKeyDownCode = (SHORT)0x8001;
    bool nextSettingPressed = GetAsyncKeyState(VK_DOWN) == onKeyDownCode;
    bool prevSettingPressed = GetAsyncKeyState(VK_UP) == onKeyDownCode;
    bool increasePressed = GetAsyncKeyState(VK_RIGHT) == onKeyDownCode;
    bool decreasePressed = GetAsyncKeyState(VK_LEFT) == onKeyDownCode;

    static bool useAdvancedSettingsMode = false;
    static int selectedSettingNum = 0;

    if (forceDraw || nextSettingPressed || prevSettingPressed || increasePressed || decreasePressed)
    {
        COORD coord = { 0 };
        HANDLE h = GetStdHandle(STD_OUTPUT_HANDLE);
        SetConsoleCursorPosition(h, coord);
        std::cout << "Arrows up/down: previous/next setting. Arrows left/right: decrease/increase setting." << std::endl << std::endl;

        selectedSettingNum += nextSettingPressed ? 1 : (prevSettingPressed && selectedSettingNum > 0 ? -1 : 0);

        const int numSettings = (int)allSettings.size();
        selectedSettingNum = std::min<int>(selectedSettingNum, numSettings - 1);

        if (increasePressed || decreasePressed)
        {
            allSettings[selectedSettingNum]->ChangeValue(increasePressed);
        }
        for (int i = 0; i < numSettings; i++)
        {
            allSettings[i]->Draw(i == selectedSettingNum);
        }
    }
}
