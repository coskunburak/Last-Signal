            // Step 2: Muzzle obstruction — is there geometry between muzzle and the nearby area?
            Vector3 muzzleForward = (targetPoint - muzzlePosition).normalized;
            RaycastHit muzzleHit;
            
            // If the weapon barrel has clipped into or through a collider, the muzzle raycast will fail 
            // because it starts inside. We detect this by tracing from the camera (eye) to the muzzle.
            if (Physics.Linecast(cameraOrigin, muzzlePosition, out RaycastHit clipHit, hitMask, QueryTriggerInteraction.Ignore))
            {
                muzzleHit = clipHit;
                result.MuzzleObstructed = true;
            }
            else if (Physics.Raycast(muzzlePosition, muzzleForward, out muzzleHit, muzzleObstructionRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                // The nearest obstruction receives the shot, including a damageable at contact range.
                // Never continue through it to the camera-selected distant target.
                result.MuzzleObstructed = true;
            }
            else
            {
                float fullDistance = Vector3.Distance(muzzlePosition, targetPoint) + .1f;
                if (!Physics.Raycast(muzzlePosition, muzzleForward, out muzzleHit, fullDistance, hitMask, QueryTriggerInteraction.Ignore))
                    return result;
            }
