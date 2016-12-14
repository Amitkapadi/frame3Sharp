﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    class GamepadMultiSelectBehavior : StandardInputBehavior
    {
        FContext scene;
        SceneObject selectSO;

        public GamepadMultiSelectBehavior(FContext scene)
        {
            this.scene = scene;
            Priority = 10;
        }

        public override InputDevice SupportedDevices {
            get { return InputDevice.Gamepad; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            selectSO = null;
            if (input.bLeftTriggerPressed || input.bAButtonPressed) {
                SORayHit rayHit;
                if (scene.Scene.FindSORayIntersection(input.vGamepadWorldRay, out rayHit)) 
                    return CaptureRequest.Begin(this);
            }
            return CaptureRequest.Ignore;
        }


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            selectSO = null;
            SORayHit rayHit;
            if (scene.Scene.FindSORayIntersection(input.vGamepadWorldRay, out rayHit)) {
                selectSO = rayHit.hitSO;
                return Capture.Begin(this);
            }
            return Capture.Ignore;
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if ( input.bLeftTriggerReleased || input.bAButtonReleased ) { 

                SORayHit rayHit;
                if ( selectSO != null && selectSO.FindRayIntersection(input.vGamepadWorldRay, out rayHit)) {
                    if (input.bAButtonDown) {
                        if (scene.Scene.IsSelected(selectSO))
                            scene.Scene.Deselect(selectSO);
                        else
                            scene.Scene.Select(selectSO, false);
                    } else
                        scene.Scene.Select(selectSO, true);
                }
                selectSO = null;
                return Capture.End;

            } else {
                return Capture.Continue;
            }
        }

        public override Capture ForceEndCapture(InputState input, CaptureData data) {
            return Capture.End;
        }

    }
}
