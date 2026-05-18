namespace ImGuiOverlay.ABI
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI OFFSETS  ，  Arena Breakout Infinite engine + game pointers
    //  Last verified: current build
    // /////////////////////////////////////////////////////////////////////////////////////

    public static class ABIOffsets
    {
        // //// GObjects / GNames / GWorld ///////////////////////////////////////////////
        public const ulong GWorld      = 0xA0427A8;
        public const ulong GNames      = 0xA574D80;
        public const ulong GObjects    = 0xA292588;
        public const ulong DecryptKey  = 0xA52D2DC;

        // //// UWorld ///////////////////////////////////////////////////////////////////
        public const ulong UWorld_OwningGameInstance = 0x180;
        public const ulong UWorld_GameState          = 0x120;
        public const ulong UWorld_PersistentLevel    = 0x30;

        // //// UGameInstance ///////////////////////////////////////////////////////////
        public const ulong UGameInstance_LocalPlayers = 0x38;
        public const ulong UPlayer_PlayerController   = 0x30;

        // //// AController / APlayerController /////////////////////////////////////////
        public const ulong APlayerController_AcknowledgedPawn    = 0x390;
        public const ulong APlayerController_PlayerCameraManager = 0x3A8;
        public const ulong AController_ControlRotation           = 0x378;
        public const ulong AController_Pawn                      = 0x340;
        public const ulong AController_PlayerState               = 0x318;

        // //// AActor //////////////////////////////////////////////////////////////////
        public const ulong AActor_RootComponent = 0x168;

        // //// ULevel //////////////////////////////////////////////////////////////////
        public const ulong ULevel_ActorArray = 0x98;
        public const ulong ULevel_ActorCount = 0xA0;

        // //// AGameStateBase //////////////////////////////////////////////////////////
        public const ulong AGameStateBase_PlayerArray = 0x328;
        public const ulong AGameStateBase_PlayerCount = 0x330;

        // //// ACharacter //////////////////////////////////////////////////////////////
        public const ulong ACharacter_Mesh = 0x380;

        // //// Scene / Skeletal Components /////////////////////////////////////////////
        public const ulong USceneComponent_RelativeLocation      = 0x16C;
        public const ulong USceneComponent_ComponentToWorld      = 0x1C0;
        public const ulong USceneComponent_ComponentToWorld_Ptr  = 0x210;
        public const ulong USkeletalMeshComponent_ComponentToWorld = 0x220;

        // //// Camera //////////////////////////////////////////////////////////////////
        public const ulong APlayerCameraManager_CameraCachePrivate            = 0x2090;
        public const ulong APlayerCameraManager_LastFrameCameraCachePrivate   = 0x27C0;

        // //// Skeletal Mesh Bones /////////////////////////////////////////////////////
        public const ulong USkeletalMeshComponent_CachedComponentSpaceTransforms = 0x08F8;
        public const ulong USkeletalMeshComponent_ComponentSpaceTransforms       = 0x08F8;

        // //// ASGInventory (SDK-verified) /////////////////////////////////////////////
        public const ulong ASGInventory_ItemId              = 0x06A8;  // uint64
        public const ulong ASGInventory_CommonDataComponent = 0x0750;  // USGInventoryCommonDataComponent*
        public const ulong ASGInventory_InventoryType       = 0x0758;  // ESGInventoryType (uint8, SDK-verified)

        // //// ABP_ContainerBase_C extends ASGInventory at 0x08A0 (SDK-verified) //////
        public const ulong Container_SGInventoryCommonData  = 0x08A8;  // USGInventoryCommonDataComponent*
        public const ulong Container_ContainerMgr           = 0x08D8;  // USGInventoryContainerMgrComponent*

        // //// USGInventoryCommonDataComponent (SDK-verified) //////////////////////////
        public const ulong CDC_TotalCount                   = 0x0104;  // int32  stack size
        public const ulong CDC_StandardPrice                = 0x010C;  // int32
        public const ulong CDC_Rarity                       = 0x0110;  // int32
        public const ulong CDC_SellPrice                    = 0x0134;  // uint32
        public const ulong CDC_DisplayName                  = 0x0140;  // FText (0x18) ！ first qword = FTextData*
        public const ulong CDC_SimpleDisplayName            = 0x0170;  // FText (0x18)

        // //// USGInventoryContainerMgrComponent (SDK-verified) ///////////////////////
        public const ulong CMgr_InventoryContainerBaseList  = 0x0140;  // TArray<FInventoryContainerBase> ！ struct not yet dumped
        public const ulong CMgr_IsRolledUp                  = 0x0150;  // bool
    }

    // /////////////////////////////////////////////////////////////////////////////////////
    //  EXTENDED OFFSETS  ，  Vitals, death, weapon zoom
    // /////////////////////////////////////////////////////////////////////////////////////

internal static class ABIOffsetsExt
{
    // ???? Pawn ????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
    public const int OFF_PAWN_ASC              = 0x1638;  // UAbilitySystemComponent*       ? verified
    public const int OFF_PAWN_DEATHCOMP        = 0x1780;  // USGCharacterDeathComponent*    ? verified
    public const int OFF_PAWN_WEAPONMAN        = 0x1778;  // USGCharacterWeaponManagerComponent* ? verified
    public const int OFF_CHAR_TICKING_ON_DEATH = 0x1708;  // bool bTickingOnDeath           ? verified

    // ???? AbilitySystemComponent ????????????????????????????????????????????????????????????????????????????????????????????
    public const int OFF_ASC_ATTRSETS          = 0x0188;  // TArray<UAttributeSet*>         ? verified

    // ???? Health AttributeSet ??????????????????????????????????????????????????????????????????????????????????????????????????
    // Still need USGHealthAttributeSet / USGCharacterHealthAttributeSet dump to confirm
    public const int OFF_ATTR_HEALTH           = 0x48;
    public const int OFF_ATTR_HEALTHMAX        = 0x4C;

    // ???? Death Component ??????????????????????????????????????????????????????????????????????????????????????????????????????????
    public const int OFF_DEATHCOMP_DEATHINFO   = 0x0240;  // FCharacterDeathInfo DeathInfo  ? verified

    // ???? Mesh visibility timers (UPrimitiveComponent, standard UE) ??????????????????????
    public const ulong OFF_MESH_TIMERS         = 0x3FC;
    public const int   OFF_LASTSUBMIT          = 0x0;
    public const int   OFF_LASTONSCREEN        = 0x4;
    public const float VIS_TICK               = 0.06f;

    //MeshTimers is unkown located in UPrimitiveComponent
    //unsigned char                                      UnknownData15_6[0x8];                                       // 0x03F0   (0x0008)  MISSED
	//float                                              BoundsScale;                                                // 0x03F8   (0x0004)  
	//unsigned char                                      UnknownData16_6[0xC];                                       // 0x03FC   (0x000C)  //OFF_MESH_TIMERS
	//TArray<class AActor*>                              MoveIgnoreActors;                                           // 0x0408   (0x0010)  
	//TArray<class UPrimitiveComponent*>                 MoveIgnoreComponents;                                       // 0x0418   (0x0010)  
	//unsigned char                                      UnknownData17_6[0x10];                                      // 0x0428   (0x0010)  MISSED

    // ???? Weapon Manager ????????????????????????????????????????????????????????????????????????????????????????????????????????????
    public const ulong OFF_WEAPON_CURRENT      = 0x1F8;   // ASGInventory* CurrentWeapon    ? verified (was 0x158 ?)

    // ???? Weapon / Zoom (need ASGWeapon / ASGInventory dump to verify) ??????????????????
    public const ulong OFF_WEAPON_ZOOMCOMP     = 0xB00;
    public const ulong OFF_ZOOM_PROGRESSRATE   = 0x404;
    public const ulong OFF_ZOOM_SCOPEMAG       = 0x578;
}
}