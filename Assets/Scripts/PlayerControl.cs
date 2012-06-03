#define ARCADE_MODE

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class PlayerControl : MonoBehaviour
{
    const float HitDistance = 1f;
    const float BodySize = 2f;
    const float WalledImpulse = 30;
    const float DashImpulse = 15;
    const float HitForceImpulse = 25;
    const float HitRecoilImpulse = 10;
    const float Cooldown = 0.25f;
    const float AutoBalancingFactor = 90;
    const float StunTime = 0.5f;
    const float HitTorque = 0.1f;

    static readonly Dictionary<string, Dictionary<Direction, string[]>> ButtonMap = new Dictionary<string, Dictionary<Direction, string[]>>
    {
        { "Controller (XBOX 360 For Windows)", new Dictionary<Direction, string[]>(DirectionComparer.Default)
            {
                { Direction.Up, new [] { "1-" } },
                { Direction.Down, new [] { "4-" } },
                { Direction.Left, new [] { "2-" } },
                { Direction.Right, new [] { "3-" } },
            } 
        },
        { "Controller (Xbox 360 Wireless Receiver for Windows)", new Dictionary<Direction, string[]>(DirectionComparer.Default)
            {
                { Direction.Up, new [] { "1-" } },
                { Direction.Down, new [] { "4-" } },
                { Direction.Left, new [] { "2-" } },
                { Direction.Right, new [] { "3-" } },
            } 
        },
        { string.Empty, new Dictionary<Direction, string[]>(DirectionComparer.Default)
            {
                { Direction.Up, new [] { "7-", "17-" } },
                { Direction.Down, new [] { "6-", "20-" } },
                { Direction.Left, new [] { "9-", "18-" } },
                { Direction.Right, new [] { "8-", "19-" } },
            } 
        },		
        { "Other", new Dictionary<Direction, string[]>(DirectionComparer.Default)
            {
                { Direction.Up, new [] { "2-" } },
                { Direction.Down, new [] { "4-" } },
                { Direction.Left, new [] { "3-" } },
                { Direction.Right, new [] { "1-" } },
            } 
        },
#if ARCADE_MODE
        { "Arcade", new Dictionary<Direction, string[]>(DirectionComparer.Default)
            {
                { Direction.Up, new [] { "Down-" } },
                { Direction.Down, new [] { "Up-" } },
                { Direction.Left, new [] { "Right-" } },
                { Direction.Right, new [] { "Left-" } },
            } 
        }
#endif
    };

    public int PlayerIndex;
    public Material IdleMaterial, PainMaterial, DizzyMaterial;
    public List<GameObject> PossibleAttachments;
    public AudioClip HighHitSound, MediumHitSound, LowHitSound, GroundSound, MissSound;

    // Fx prefabs
    public GameObject HitFx, LandedHitFx, HdCircleFx;

    public bool Grounded;

    // Doors
    GameObject OuterLeft, OuterRight, InnerLeft, InnerRight;

    // Achievements
    GameObject ComboAch, DashAch, PowerAch;

    GameObject Logo;

    ComplexButtonState UpState, DownState, LeftState, RightState;
    float ArmCooldown, LegCooldown;
    float StunCooldown;
    int StunHits;
    GameObject HurtBubble, DeadBubble;
    Dictionary<Direction, string[]> ControllerMap;
    readonly HashSet<string> DownButtons = new HashSet<string>();
    GameObject StrayObjects;
    Vector3 CameraOrigin;
    bool IsLastHitLanded;
    float LastHitForce;
    float DeathTimer = 7;
    public int MidAirHits;
    float SinceInput;

    readonly List<GameObject> AttachmentPoints = new List<GameObject>();
    readonly Dictionary<Direction, GameObject[]> Arms = new Dictionary<Direction, GameObject[]>(DirectionComparer.Default);
    readonly Dictionary<Direction, GameObject> Legs = new Dictionary<Direction, GameObject>(DirectionComparer.Default);
    readonly Dictionary<Direction, GameObject> Ears = new Dictionary<Direction, GameObject>(DirectionComparer.Default);

    void Start()
    {
        Arms.Add(Direction.Up, new [] { gameObject.FindChild("ArmLeftUp"), gameObject.FindChild("ArmRightUp") });
        Arms.Add(Direction.Down, new[] { gameObject.FindChild("ArmLeftDown"), gameObject.FindChild("ArmRightDown") });

        Legs.Add(Direction.Left, gameObject.FindChild("LegLeft"));
        Legs.Add(Direction.Right, gameObject.FindChild("LegRight"));

        Ears.Add(Direction.Left, gameObject.FindChild("EarLeft"));
        Ears.Add(Direction.Right, gameObject.FindChild("EarRight"));

        HurtBubble = gameObject.FindChild("Hurt");
        DeadBubble = gameObject.FindChild("Dead");

        StrayObjects = GameObject.Find("Stray Objects");

        OuterLeft = GameObject.Find("OuterLeft");
        OuterRight = GameObject.Find("OuterRight");
        InnerLeft = GameObject.Find("InnerLeft");
        InnerRight = GameObject.Find("InnerRight");
        Logo = GameObject.Find("Logo");

        ComboAch = GameObject.Find("ComboAch");
        DashAch = GameObject.Find("DashAch");
        PowerAch = GameObject.Find("PowerAch");

        foreach (var l in Legs.Values) { l.transform.position += new Vector3(0, 1, 0); l.renderer.enabled = false; }
        foreach (var e in Ears.Values) { e.transform.position += new Vector3(0, -1, 0); e.renderer.enabled = false; }
        foreach (var armSet in Arms.Values)
        {
            armSet[0].transform.position += new Vector3(1, 0, 0);
            armSet[1].transform.position -= new Vector3(1, 0, 0);
            armSet[0].renderer.enabled = armSet[1].renderer.enabled = false;
        }

        for (int i = 0; i < 9; i++)
        {
        lulz:
            GameObject peg = null;
            while (peg == null || AttachmentPoints.Contains(peg))
                peg = gameObject.FindChild("Peg" + RandomHelper.Random.Next(0, 9));
            var attach = RandomHelper.InEnumerable(PossibleAttachments);
            if ((attach.name == "Bamboo" || attach.name == "Pickaxe") && !(peg.name.EndsWith("0") || peg.name.EndsWith("1") || peg.name.EndsWith("2")))
                goto lulz;
            PossibleAttachments.Remove(attach);
            var item = Instantiate(attach) as GameObject;
            item.transform.position = peg.transform.position;
            item.transform.rotation = peg.transform.rotation;
            item.transform.localScale = new Vector3(RandomHelper.Between(0.8, 1.1), RandomHelper.Between(0.8, 1.1), RandomHelper.Between(0.8, 1.1));
            item.transform.parent = peg.transform;
            item.AddComponent<LazyFollow>();
            AttachmentPoints.Add(peg);
        }

        if (Input.GetJoystickNames().Length >= PlayerIndex)
        {
            var controllerName = Input.GetJoystickNames()[PlayerIndex - 1];

            //Debug.Log("Player #" + PlayerIndex + " has '" + controllerName + "'");
			
            if (!ButtonMap.TryGetValue(controllerName, out ControllerMap))
                ControllerMap = ButtonMap["Other"];
        }
#if ARCADE_MODE
        else
        {
            // Fallback to arcade keyboard controls
            ControllerMap = ButtonMap["Arcade"];
        }
#endif

        CameraOrigin = Camera.main.transform.position;

        Screen.showCursor = false;
    }

    void Update()
    {
        if (ControllerMap == null) return;
        foreach (var button in ControllerMap.Values.SelectMany(x => x))
        {
            var fullButton = button + PlayerIndex;
            if (Input.GetButtonDown(fullButton) && !DownButtons.Contains(fullButton))
                DownButtons.Add(fullButton);
        }

        audio.pitch = Mathf.Lerp(Time.timeScale, 1, 0.5f);

#if ARCADE_MODE
        SinceInput += Time.deltaTime;
        if (DownButtons.Count > 0)
            SinceInput = 0;
        if (Input.GetKeyDown(KeyCode.Alpha9) || SinceInput > 60 * 5)
            Application.Quit();
#endif
    }

    void FixedUpdate()
    {
        var vertical = Input.GetAxis("Vertical" + PlayerIndex);
        var horizontal = Input.GetAxis("Horizontal" + PlayerIndex);

        // Base rotation
        var angle = -transform.localRotation.eulerAngles.z;

        // Death
        if (AttachmentPoints.Count == 0)
        {
            DeadBubble.transform.rotation = Quaternion.Euler(90, 0, 0);
            DeadBubble.transform.position = transform.position + new Vector3(PlayerIndex == 1 ? -1 : 1, 1.5f, 0);
            if (PlayerIndex == 2)
                DeadBubble.transform.localScale = new Vector3(1, -1, 1);
            else
                DeadBubble.transform.localScale = new Vector3(-1, 1, 1);

            DeathTimer -= Time.deltaTime;
            var cheapo = DeathTimer * 1.5f;
            DeadBubble.renderer.enabled = (cheapo - Math.Floor(cheapo)) > 0.5f;

            if (DeathTimer <= 0)
            {
                enabled = false;
                DeadBubble.renderer.enabled = false;

                const float SlideDuration = 0.75f;

                Wait.Until(elapsed =>
                {
                    var step = Easing.EaseIn(Mathf.Clamp01(elapsed / SlideDuration), EasingType.Quadratic);
                    InnerLeft.transform.localPosition = OuterLeft.transform.localPosition = 
                        Vector3.Lerp(new Vector3(-6, 0, 0), new Vector3(0, 0, 0), step);
                    OuterRight.transform.localPosition = InnerRight.transform.localPosition =
                        Vector3.Lerp(new Vector3(25f, 0, 0), new Vector3(18.5f, 0, 0), step);
                    return step >= 1;
                }, () =>
                    Wait.Until(elapsed =>
                    {
                        var step = Easing.EaseIn(Mathf.Clamp01(elapsed / SlideDuration), EasingType.Quadratic);
                        InnerLeft.transform.localPosition = Vector3.Lerp(new Vector3(0, 0, 0.125f), new Vector3(6.17f, 0, 0.125f), step);
                        InnerRight.transform.localPosition = Vector3.Lerp(new Vector3(18.5f, 0, 0.125f), new Vector3(12.34f, 0, 0.125f), step);
                        Logo.renderer.material.color = new Color(1, 1, 1, step);
                        return step >= 1;
                    }, () =>
                        Wait.Until(e =>
                        {
                            foreach (var r in StrayObjects.GetComponentsInChildren<Renderer>())
                                r.material.color = new Color(1, 1, 1, 1 - e);
                            return e >= 1;
                        }, () => Application.LoadLevel(0)))
                );

                return;
            }
        }

        // Stunning!
        if (StunCooldown > 0)
        {
            StunCooldown = Mathf.Max(0, StunCooldown - Time.deltaTime);
            HurtBubble.transform.rotation = Quaternion.Euler(90, 0, 0);
            HurtBubble.transform.position = transform.position + new Vector3(PlayerIndex == 1 ? -1 : 1, 1.5f, 0);
            if (PlayerIndex == 2)
                HurtBubble.transform.localScale = new Vector3(1, -1, 1);
            else
                HurtBubble.transform.localScale = new Vector3(-1, 1, 1);

            if (StunCooldown == 0)
            {
                renderer.material = AttachmentPoints.Count > 0 ? IdleMaterial : DizzyMaterial;
                HurtBubble.renderer.enabled = false;
            }
            return;
        }
        StunHits = 0;

        //var one = Input.GetButtonDown("1-" + PlayerIndex);
        //var two = Input.GetButtonDown("2-" + PlayerIndex);
        //var three = Input.GetButtonDown("3-" + PlayerIndex);
        //var four = Input.GetButtonDown("4-" + PlayerIndex);

        //if (one) Debug.Log("1-" + PlayerIndex + " ~ " + Input.GetJoystickNames()[PlayerIndex - 1]);
        //if (two) Debug.Log("2-" + PlayerIndex + " ~ " + Input.GetJoystickNames()[PlayerIndex - 1]);
        //if (three) Debug.Log("3-" + PlayerIndex + " ~ " + Input.GetJoystickNames()[PlayerIndex - 1]);
        //if (four) Debug.Log("4-" + PlayerIndex + " ~ " + Input.GetJoystickNames()[PlayerIndex - 1]);

        if (ControllerMap == null) return;

        if (vertical > 0.5f && UpState == ComplexButtonState.Up) { UpState = ComplexButtonState.Down; DoAppropriateAttack(angle + 180); }
        if (UpState == ComplexButtonState.Down && vertical < 0.5f) UpState = ComplexButtonState.Up;
        foreach (var button in ControllerMap[Direction.Down])
            if (DownButtons.Contains(button + PlayerIndex)) DoAppropriateAttack(angle + 180);
        //if (Keyboard.GetKeyState(HitDownKey).State == ComplexButtonState.Pressed)

        if (vertical < -0.5f && DownState == ComplexButtonState.Up) { DownState = ComplexButtonState.Down; DoAppropriateAttack(angle); }
        if (DownState == ComplexButtonState.Down && vertical > -0.5f) DownState = ComplexButtonState.Up;
        foreach (var button in ControllerMap[Direction.Up])
            if (DownButtons.Contains(button + PlayerIndex)) DoAppropriateAttack(angle);
        //if (Keyboard.GetKeyState(HitUpKey).State == ComplexButtonState.Pressed)

        if (horizontal < -0.5f && LeftState == ComplexButtonState.Up) { LeftState = ComplexButtonState.Down; DoAppropriateAttack(angle - 90); }
        if (LeftState == ComplexButtonState.Down && horizontal > -0.5f) LeftState = ComplexButtonState.Up;
        foreach (var button in ControllerMap[Direction.Left])
            if (DownButtons.Contains(button + PlayerIndex)) DoAppropriateAttack(angle - 90);
        //if (Keyboard.GetKeyState(HitLeftKey).State == ComplexButtonState.Pressed)

        if (horizontal > 0.5f && RightState == ComplexButtonState.Up) { RightState = ComplexButtonState.Down; DoAppropriateAttack(angle + 90); }
        if (RightState == ComplexButtonState.Down && horizontal < 0.5f) RightState = ComplexButtonState.Up;
        foreach (var button in ControllerMap[Direction.Right])
            if (DownButtons.Contains(button + PlayerIndex)) DoAppropriateAttack(angle + 90);
        //if (Keyboard.GetKeyState(HitRightKey).State == ComplexButtonState.Pressed)

        // Try to keep upright
        var currentUp = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle));
        var targetUp = new Vector2(1, 0) * (AttachmentPoints.Count > 0 ? 1 : -1);
        var autoBalancing = Easing.EaseIn((2 - (Vector2.Dot(targetUp, currentUp) + 1)) / 2, EasingType.Sine);
        var sign = Mathf.Sign(targetUp.y - currentUp.y) * (AttachmentPoints.Count > 0 ? 1 : -1);
        if (sign == 0) sign = 1;
        rigidbody.AddTorque(Vector3.forward * autoBalancing * sign * AutoBalancingFactor, ForceMode.Force);

        DownButtons.Clear();
    }

    void DoAppropriateAttack(float angle)
    {
        // Wrap angle to [0, 360]
        angle = angle % 360;
        if (angle < 0) angle += 360;

        // Choose member!
        if (angle <= 45 || angle > 315)
        {
            if (AttachmentPoints.Count > 0)
                EarAttack();
        }
        else if (angle > 45 && angle <= 135)
            ArmAttack(Direction.Right);
        else if (angle > 135 && angle <= 225)
            LegAttack();
        else /* if (angle > 225 && angle <= 315) */
            ArmAttack(Direction.Left);
    }

    void ArmAttack(Direction hitDirection)
    {
        var upIsBusy = Arms[Direction.Up][0].renderer.enabled || Arms[Direction.Up][1].renderer.enabled;
        var downIsBusy = Arms[Direction.Down][0].renderer.enabled || Arms[Direction.Down][1].renderer.enabled;

        Direction armHeight;
        if (upIsBusy && downIsBusy) armHeight = Direction.None;
        else if (!upIsBusy && !downIsBusy) armHeight = Direction.Down;
        else if (upIsBusy) armHeight = Direction.Down;
        else /* if (downIsBusy) */ armHeight = Direction.Up;

        if (armHeight != Direction.None)
        {
            var armToUse = Arms[armHeight][hitDirection == Direction.Left ? 0 : 1];
            if (armToUse.renderer.enabled) return;

            var hitVector = Quaternion.AngleAxis(-transform.localRotation.eulerAngles.z, Vector3.forward) * hitDirection.ToVector();
            var offset = new Vector3(0, armToUse.transform.localPosition.y, 0);

            var impulse = ComputeForce(hitVector, offset);
            DoHitFx(-transform.localRotation.eulerAngles.z + (hitDirection == Direction.Left ? 90 : -90), hitVector, offset);
            rigidbody.AddForce(-hitVector * impulse, ForceMode.Impulse);

            armToUse.renderer.enabled = true;

            var initialOffset = new Vector3(hitDirection.ToVector().x * -1, 0, 0);
            var origin = armToUse.transform.localPosition;
            armToUse.transform.localPosition = origin + initialOffset;

            Wait.Until(elapsed =>
            {
                var step = Easing.EaseOut(1 - Mathf.Clamp01(elapsed / Cooldown), EasingType.Cubic);
                armToUse.transform.localPosition = origin + initialOffset * step;
                return step == 0;
            },
            () => { armToUse.renderer.enabled = false; });
        }
    }

    void LegAttack()
    {
        var hitDirection = Vector3.down;

        var leftIsBusy = Legs[Direction.Left].renderer.enabled;
        var rightIsBusy = Legs[Direction.Right].renderer.enabled;

        Direction legSide;
        if (leftIsBusy && rightIsBusy) legSide = Direction.None;
        else if (!leftIsBusy && !rightIsBusy) legSide = RandomHelper.Probability(0.5) ? Direction.Left : Direction.Right;
        else if (leftIsBusy) legSide = Direction.Right;
        else /* if (rightIsBusy) */ legSide = Direction.Left;

        if (legSide != Direction.None)
        {
            var hitVector = Quaternion.AngleAxis(-transform.localRotation.eulerAngles.z, Vector3.forward) * hitDirection;

            var legToUse = Legs[legSide];
            legToUse.renderer.enabled = true;

            var offset = new Vector3(-legToUse.transform.localPosition.x, 0, 0);

            var impulse = ComputeForce(hitVector, offset);
            DoHitFx(-transform.localRotation.eulerAngles.z + 180, hitVector, offset);
            rigidbody.AddForce(-hitVector * impulse, ForceMode.Impulse);

            var initialOffset = new Vector3(0, -1, 0);
            var origin = legToUse.transform.localPosition;
            legToUse.transform.localPosition = origin + initialOffset;

            Wait.Until(elapsed =>
            {
                var step = Easing.EaseOut(1 - Mathf.Clamp01(elapsed / Cooldown), EasingType.Cubic);
                legToUse.transform.localPosition = origin + initialOffset * step;
                return step <= 0;
            },
            () => { legToUse.renderer.enabled = false; });
        }
    }

    void EarAttack()
    {
        var hitDirection = Vector3.up;

        var leftIsBusy = Ears[Direction.Left].renderer.enabled;
        var rightIsBusy = Ears[Direction.Right].renderer.enabled;

        Direction earSide;
        if (leftIsBusy && rightIsBusy) earSide = Direction.None;
        else if (!leftIsBusy && !rightIsBusy) earSide = RandomHelper.Probability(0.5) ? Direction.Left : Direction.Right;
        else if (leftIsBusy) earSide = Direction.Right;
        else /* if (rightIsBusy) */ earSide = Direction.Left;

        if (earSide != Direction.None)
        {
            var earToUse = Ears[earSide];
            earToUse.renderer.enabled = true;

            var hitVector = Quaternion.AngleAxis(-transform.localRotation.eulerAngles.z, Vector3.forward) * hitDirection;
            var offset = new Vector3(-earToUse.transform.localPosition.x, 0, 0);

            var impulse = ComputeForce(hitVector, offset);
            DoHitFx(-transform.localRotation.eulerAngles.z, hitVector, offset);
            rigidbody.AddForce(-hitVector * impulse, ForceMode.Impulse);

            var initialOffset = new Vector3(0, 1, 0);
            var origin = earToUse.transform.localPosition;
            earToUse.transform.localPosition = origin + initialOffset;

            Wait.Until(elapsed =>
            {
                var step = Easing.EaseOut(1 - Mathf.Clamp01(elapsed / Cooldown), EasingType.Cubic);
                earToUse.transform.localPosition = origin + initialOffset * step;
                return step == 0;
            },
            () => { earToUse.renderer.enabled = false; });
        }
    }

    void DoHitFx(float angle, Vector3 direction, Vector3 offset)
    {
        var fx = Instantiate(IsLastHitLanded ? LandedHitFx : HitFx) as GameObject;
        fx.transform.position = transform.position + offset;
        fx.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Wait.Until(elapsed =>
        {
            var duration = IsLastHitLanded ? 0.25f : 0.125f;
            var step = Easing.EaseOut(Mathf.Clamp01(elapsed / duration), EasingType.Quadratic);
            fx.transform.localScale = Vector3.Lerp(new Vector3(0.6f, 0.6f, 0.6f), new Vector3(0.7f, 3, 0.7f) * (IsLastHitLanded ? 2 : 1), step);
            foreach (var r in fx.GetComponentsInChildren<Renderer>())
                r.material.color = new Color(1, 1, 1, 1 - step);
            return step >= 1;
        }, () => Destroy(fx));

        if (IsLastHitLanded)
        {
            var hd = Instantiate(HdCircleFx) as GameObject;
            hd.transform.position = transform.position + offset + direction * BodySize / 2;
            Wait.Until(elapsed =>
            {
                var duration = 0.125f;
                var step = Easing.EaseOut(Mathf.Clamp01(elapsed / duration), EasingType.Quadratic);
                var scaleFactor = LastHitForce;
                hd.transform.localScale = Vector3.Lerp(Vector3.one / 4, Vector3.one, step) * (scaleFactor * 2);
                foreach (var r in hd.GetComponentsInChildren<Renderer>())
                    r.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f - step / 2));
                return step >= 1;
            }, () => Destroy(hd));
        }
    }

    bool AlreadyWaitingForHit;
    bool AnotherHit;

    float ComputeForce(Vector3 direction, Vector3 offset)
    {
        var ray = new Ray
        {
            direction = direction,
            origin = transform.position + BodySize / 2 * direction - direction * 0.1f + offset
        };
        RaycastHit info;
        var collided = Physics.Raycast(ray, out info, HitDistance) && info.collider != collider;

        IsLastHitLanded = false;

        // Enemy recoil
        if (info.rigidbody != null && info.rigidbody != rigidbody && info.rigidbody.gameObject.tag == "Player")
        {
            var combinedVelocity = rigidbody.velocity - info.rigidbody.velocity;
            var damageFactor = Mathf.Clamp01(Vector3.Dot(combinedVelocity, direction) / 30) * 2 + 0.65f;

            BulletTimeManager.Instance.AddBulletTime(damageFactor);
            IsLastHitLanded = true;
            LastHitForce = damageFactor;

            info.rigidbody.AddForce(direction * HitForceImpulse * damageFactor, ForceMode.Impulse);
            info.rigidbody.AddTorque(Vector3.forward * RandomHelper.Sign() * HitTorque * damageFactor, ForceMode.Impulse);
            info.rigidbody.gameObject.GetComponent<PlayerControl>().Stun(damageFactor);

            CamShake(direction * damageFactor);

            // Try to wait for another hit
            if (AlreadyWaitingForHit)
                AnotherHit = true;
            else
            {
                MidAirHits++;
                if (MidAirHits >= 2)
                {
                    MidAirHits = 0;
                    //// COMBO ACHIEVEMENT
                    //Wait.Until(elapsed =>
                    //{
                    //    var step = Easing.EaseIn(Mathf.Clamp01(elapsed / 0.25f), EasingType.Quadratic);
                    //    ComboAch.transform.localPosition =
                    //        Vector3.Lerp(new Vector3(2.77f, 0, 0), new Vector3(7.5f, 0, 0), step);
                    //    return step >= 1;
                    //}, () =>
                    //       {
                    //           Wait.Until(elapsed => elapsed > 1, () =>
                    //           {
                    //               Wait.Until(elapsed =>
                    //               {
                    //                   var step = Easing.EaseIn(Mathf.Clamp01(elapsed / 0.25f), EasingType.Quadratic);
                    //                   ComboAch.transform.localPosition =
                    //                       Vector3.Lerp(new Vector3(7.5f, 0, 0), new Vector3(2.77f, 0, 0), step);
                    //                   return step >= 1;
                    //               }, () =>
                    //               {
                    //               }, true);
                    //           }, true);
                    //       }, true);
                }
                AlreadyWaitingForHit = true;
                Wait.Until(e => e > 0.05f, () =>
                {
                    audio.PlayOneShot(AnotherHit ? HighHitSound : MediumHitSound);
                    AlreadyWaitingForHit = AnotherHit = false;
                }, true);
            }

            return HitRecoilImpulse;
        }

        if (collided)
            audio.PlayOneShot(LowHitSound, 0.7f);
        else
            audio.PlayOneShot(MissSound, 0.8725f);

        return collided ? WalledImpulse : DashImpulse;
    }

    void CamShake(Vector3 direction)
    {
        var mag = direction.magnitude * 1.1f;

        foreach (var rb in StrayObjects.GetComponentsInChildren<Rigidbody>())
            rb.AddForce(direction * -5, ForceMode.Impulse);

        Wait.Until(elapsed =>
        {
            var step = Easing.EaseIn(1 - Mathf.Clamp01(elapsed / (0.25f * mag)), EasingType.Quadratic);
            Camera.main.transform.position = CameraOrigin + VectorEx.Modulate(UnityEngine.Random.insideUnitSphere, (direction + Vector3.one * mag) / 2) * step;
            return step <= 0;
        }, true);
    }

    public void Stun(float stunFactor)
    {
        StunHits++;
        if (BulletTimeManager.Instance.TotalDuration > 0.4f && AttachmentPoints.Count > 0)
        {
            // Unhook a peg's items
            var peg = RandomHelper.InEnumerable(AttachmentPoints);
            Destroy(peg.GetComponentInChildren<LazyFollow>());
            peg.transform.parent = StrayObjects.transform;
            AttachmentPoints.Remove(peg);
            foreach (var wi in peg.GetComponentsInChildren<WeightedItem>())
            {
                wi.gameObject.AddComponent<Rigidbody>();
                wi.rigidbody.mass = wi.GetComponentInChildren<WeightedItem>().Mass * RandomHelper.Between(0.9f, 1.1f);
                wi.rigidbody.velocity = rigidbody.velocity * 2;
            }
            foreach (var c in peg.GetComponentsInChildren<Collider>())
                c.enabled = true;

            if (AttachmentPoints.Count == 0)
            {
                renderer.material = AttachmentPoints.Count > 0 ? IdleMaterial : DizzyMaterial;
                return;
            }

            HurtBubble.renderer.enabled = true;
        }
        if (StunHits <= 2)
            StunCooldown += StunTime * stunFactor * (AttachmentPoints.Count > 0 ? 1 : 4);
        renderer.material = PainMaterial;
    }

    void OnCollisionEnter(Collision collision)
    {
        var direction = collision.impactForceSum.normalized;
        var mag = Easing.EaseIn(Mathf.Clamp01(collision.impactForceSum.magnitude / 75), EasingType.Quadratic);
        CamShake(direction * -mag / 3);

        Grounded = true;

        audio.PlayOneShot(GroundSound, mag);

        if (StunCooldown > 0 || collision.rigidbody == null || collider.gameObject.tag != "Player") return;

        var damageFactor = Mathf.Clamp01(Vector3.Dot(rigidbody.velocity, Vector3.one) / 30);
        if (damageFactor < 0.25) return;

        try
        {
            collision.rigidbody.AddForce(rigidbody.velocity.normalized * HitForceImpulse * damageFactor, ForceMode.Impulse);
            collision.rigidbody.AddTorque(Vector3.forward * RandomHelper.Sign() * HitTorque * damageFactor, ForceMode.Impulse);
            //collision.gameObject.GetComponent<PlayerControl>().Stun(damageFactor);
            //BulletTimeManager.Instance.AddBulletTime(damageFactor);
        } 
        catch (Exception) { /* ??? */ }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider.rigidbody != null) return;

        Grounded = false;
        MidAirHits = 0;
    }
}
