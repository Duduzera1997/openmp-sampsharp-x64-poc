#include <sdk.hpp>
#include <Server/Components/Actors/actors.hpp>
#include <Server/Components/Checkpoints/checkpoints.hpp>
#include <Server/Components/Classes/classes.hpp>
#include <Server/Components/Console/console.hpp>
#include <Server/Components/CustomModels/custommodels.hpp>
#include <Server/Components/Dialogs/dialogs.hpp>
#include <Server/Components/Fixes/fixes.hpp>
#include <Server/Components/GangZones/gangzones.hpp>
#include <Server/Components/LegacyConfig/legacyconfig.hpp>
#include <Server/Components/Menus/menus.hpp>
#include <Server/Components/Objects/objects.hpp>
#include <Server/Components/Pickups/pickups.hpp>
#include <Server/Components/Recordings/recordings.hpp>
#include <Server/Components/TextDraws/textdraws.hpp>
#include <Server/Components/TextLabels/textlabels.hpp>
#include <Server/Components/Vehicles/vehicles.hpp>

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wgnu-zero-variadic-macro-arguments"
#pragma clang diagnostic ignored "-Wreturn-type-c-linkage"

// macros for definition of exported proxy functions
#define _EXPAND_PARAM1(type, ...) type _1
#define _EXPAND_PARAM2(type, ...) type _2, _EXPAND_PARAM1(__VA_ARGS__)
#define _EXPAND_PARAM3(type, ...) type _3, _EXPAND_PARAM2(__VA_ARGS__)
#define _EXPAND_PARAM4(type, ...) type _4, _EXPAND_PARAM3(__VA_ARGS__)
#define _EXPAND_PARAM5(type, ...) type _5, _EXPAND_PARAM4(__VA_ARGS__)
#define _EXPAND_PARAM6(type, ...) type _6, _EXPAND_PARAM5(__VA_ARGS__)
#define _EXPAND_PARAM7(type, ...) type _7, _EXPAND_PARAM6(__VA_ARGS__)
#define _EXPAND_PARAM8(type, ...) type _8, _EXPAND_PARAM7(__VA_ARGS__)
#define _EXPAND_PARAM9(type, ...) type _9, _EXPAND_PARAM8(__VA_ARGS__)
#define _EXPAND_PARAM10(type, ...) type _10, _EXPAND_PARAM9(__VA_ARGS__)

#define _EXPAND_PARAM_N(_1, _2, _3, _4, _5, _6, _7, _8, _9, _10, n, ...) _EXPAND_PARAM ## n
#define _EXPAND_PARAM(...) _EXPAND_PARAM_N(__VA_ARGS__,10,9,8,7,6,5,4,3,2,1,0)(__VA_ARGS__)

#define _EXPAND_ARG1(type, ...) _1
#define _EXPAND_ARG2(type, ...) _2, _EXPAND_ARG1(__VA_ARGS__)
#define _EXPAND_ARG3(type, ...) _3, _EXPAND_ARG2(__VA_ARGS__)
#define _EXPAND_ARG4(type, ...) _4, _EXPAND_ARG3(__VA_ARGS__)
#define _EXPAND_ARG5(type, ...) _5, _EXPAND_ARG4(__VA_ARGS__)
#define _EXPAND_ARG6(type, ...) _6, _EXPAND_ARG5(__VA_ARGS__)
#define _EXPAND_ARG7(type, ...) _7, _EXPAND_ARG6(__VA_ARGS__)
#define _EXPAND_ARG8(type, ...) _8, _EXPAND_ARG7(__VA_ARGS__)
#define _EXPAND_ARG9(type, ...) _9, _EXPAND_ARG8(__VA_ARGS__)
#define _EXPAND_ARG10(type, ...) _10, _EXPAND_ARG9(__VA_ARGS__)

#define _EXPAND_ARG_N(_1, _2, _3, _4, _5, _6, _7, _8, _9, _10, n, ...) _EXPAND_ARG ## n
#define _EXPAND_ARG(...) _EXPAND_ARG_N(__VA_ARGS__,10,9,8,7,6,5,4,3,2,1,0)(__VA_ARGS__)

#define PROXY_OVERLOAD(type_subject, type_return, method, overload, ...); \
    extern "C" SDK_EXPORT type_return __CDECL \
    type_subject##_##method##overload(type_subject * subject __VA_OPT__(, _EXPAND_PARAM(__VA_ARGS__))) \
    { \
        return subject -> method ( \
            __VA_OPT__(_EXPAND_ARG(__VA_ARGS__)) \
        ); \
    }

#define PROXY(type_subject, type_return, method, ...) PROXY_OVERLOAD(type_subject, type_return, method,, __VA_ARGS__)

// Type aliases to prevent them from breaking proxy macros
using IntPair = Pair<int, int>;
using BoolStringPair = Pair<bool, StringView>;
using HoursMinutesPair = Pair<Hours, Minutes>;
using NewConnectionPlayerPair = Pair<NewConnectionResult, IPlayer*>;
using CarriagesArray = StaticArray<IVehicle*, MAX_VEHICLE_CARRIAGES>;
using VehicleModelArray = StaticArray<uint8_t, MAX_VEHICLE_MODELS>;
using SkillsArray = StaticArray<uint16_t, NUM_SKILL_LEVELS>;

// regex for stripping most text from struct definitions: (^(\s*\/\/\/?.*|\s*)\r?\n| const = 0| = 0| ?\w+(?=[),]))

// include/Server/Components/Actors
PROXY(IActor, void, setSkin, int);
PROXY(IActor, int, getSkin);
PROXY(IActor, void, applyAnimation, AnimationData&);
PROXY(IActor, const AnimationData&, getAnimation);
PROXY(IActor, void, clearAnimations);
PROXY(IActor, void, setHealth, float);
PROXY(IActor, float, getHealth);
PROXY(IActor, void, setInvulnerable, bool);
PROXY(IActor, bool, isInvulnerable);
PROXY(IActor, bool, isStreamedInForPlayer, IPlayer&);
PROXY(IActor, void, streamInForPlayer, IPlayer&);
PROXY(IActor, void, streamOutForPlayer, IPlayer&);
PROXY(IActor, const ActorSpawnData&, getSpawnData);

// todo: getEventDispatcher
PROXY(IActorsComponent, IActor*, create, int, Vector3, float);

// include/Server/Components/Checkpoints
PROXY(ICheckpointDataBase, Vector3, getPosition);
PROXY(ICheckpointDataBase, void, setPosition, Vector3&);
PROXY(ICheckpointDataBase, float, getRadius);
PROXY(ICheckpointDataBase, void, setRadius, float);
PROXY(ICheckpointDataBase, bool, isPlayerInside);
PROXY(ICheckpointDataBase, void, setPlayerInside, bool);
PROXY(ICheckpointDataBase, void, enable);
PROXY(ICheckpointDataBase, void, disable);
PROXY(ICheckpointDataBase, bool, isEnabled);

PROXY(IRaceCheckpointData, RaceCheckpointType, getType);
PROXY(IRaceCheckpointData, void, setType, RaceCheckpointType);
PROXY(IRaceCheckpointData, Vector3, getNextPosition);
PROXY(IRaceCheckpointData, void, setNextPosition, Vector3&);

PROXY(IPlayerCheckpointData, IRaceCheckpointData&, getRaceCheckpoint);
PROXY(IPlayerCheckpointData, ICheckpointData&, getCheckpoint);

// todo: getEventDispatcher

// include/Server/Components/Classes
PROXY(IClass, const PlayerClass&, getClass);
PROXY(IClass, void, setClass, PlayerClass&);

PROXY(IClassesComponent, IClass*, create, int, int, Vector3, float, WeaponSlots&);

// todo: getEventDispatcher

// include/Server/Components/Console

// todo: getEventDispatcher

PROXY(IConsoleComponent, void, send, StringView, ConsoleCommandSenderData&);
PROXY(IConsoleComponent, void, sendMessage, ConsoleCommandSenderData&, StringView);

PROXY(IPlayerConsoleData, bool, hasConsoleAccess);
PROXY(IPlayerConsoleData, void, setConsoleAccessibility, bool);

// include/Server/Components/CustomModels
PROXY(IPlayerCustomModelsData, uint32_t, getCustomSkin);
PROXY(IPlayerCustomModelsData, void, setCustomSkin, uint32_t);
PROXY(IPlayerCustomModelsData, bool, sendDownloadUrl, StringView);

// todo: getEventDispatcher
PROXY(ICustomModelsComponent, bool, addCustomModel, ModelType, int32_t, int32_t, StringView, StringView, int32_t, uint8_t, uint8_t);
PROXY(ICustomModelsComponent, bool, getBaseModel, uint32_t&, uint32_t&);
PROXY(ICustomModelsComponent, StringView, getModelNameFromChecksum, uint32_t);
PROXY(ICustomModelsComponent, bool, isValidCustomModel, int32_t);
PROXY(ICustomModelsComponent, bool, getCustomModelPath, int32_t, StringView&, StringView&);

// include/Server/Components/Databases
// @skip

// include/Server/Components/Dialogs
PROXY(IPlayerDialogData, void, hide, IPlayer&);
PROXY(IPlayerDialogData, void, show, IPlayer&, int, DialogStyle, StringView, StringView, StringView, StringView);
PROXY(IPlayerDialogData, void, get, int&, DialogStyle&, StringView&, StringView&, StringView&, StringView&);
PROXY(IPlayerDialogData, int, getActiveID) 

// todo: getEventDispatcher

// include/Server/Components/Fixes
PROXY(IPlayerFixesData, bool, sendGameText, StringView, Milliseconds, int);
PROXY(IPlayerFixesData, bool, hideGameText, int);
PROXY(IPlayerFixesData, bool, hasGameText, int);
PROXY(IPlayerFixesData, bool, getGameText, int, StringView&, Milliseconds&, Milliseconds&);
PROXY(IPlayerFixesData, void, applyAnimation, IPlayer*, IActor*, AnimationData*);

PROXY(IFixesComponent, bool, sendGameTextToAll, StringView, Milliseconds, int);
PROXY(IFixesComponent, bool, hideGameTextForAll, int);
PROXY(IFixesComponent, void, clearAnimation, IPlayer*, IActor*);

// include/Server/Components/GangZones
PROXY(IBaseGangZone, bool, isShownForPlayer, IPlayer&);
PROXY(IBaseGangZone, bool, isFlashingForPlayer, IPlayer&);
PROXY(IBaseGangZone, void, showForPlayer, IPlayer&, Colour&);
PROXY(IBaseGangZone, void, hideForPlayer, IPlayer&);
PROXY(IBaseGangZone, void, flashForPlayer, IPlayer&, Colour&);
PROXY(IBaseGangZone, void, stopFlashForPlayer, IPlayer&);
PROXY(IBaseGangZone, GangZonePos, getPosition);
PROXY(IBaseGangZone, void, setPosition, GangZonePos&);
PROXY(IBaseGangZone, bool, isPlayerInside, IPlayer&);
PROXY(IBaseGangZone, const FlatHashSet<IPlayer*>&, getShownFor);
PROXY(IBaseGangZone, Colour, getFlashingColourForPlayer, IPlayer&);
PROXY(IBaseGangZone, Colour, getColourForPlayer, IPlayer&);
PROXY(IBaseGangZone, void, setLegacyPlayer, IPlayer*);
PROXY(IBaseGangZone, IPlayer*, getLegacyPlayer);

PROXY(IGangZonesComponent, IGangZone*, create, GangZonePos);
PROXY(IGangZonesComponent, const FlatHashSet<IGangZone*>&, getCheckingGangZones);
PROXY(IGangZonesComponent, void, useGangZoneCheck, IGangZone&, bool);
PROXY(IGangZonesComponent, int, toLegacyID, int);
PROXY(IGangZonesComponent, int, fromLegacyID, int);
PROXY(IGangZonesComponent, void, releaseLegacyID, int);
PROXY(IGangZonesComponent, int, reserveLegacyID);
PROXY(IGangZonesComponent, void, setLegacyID, int, int);

PROXY(IPlayerGangZoneData, int, toLegacyID, int);
PROXY(IPlayerGangZoneData, int, fromLegacyID, int);
PROXY(IPlayerGangZoneData, void, releaseLegacyID, int);
PROXY(IPlayerGangZoneData, int, reserveLegacyID);
PROXY(IPlayerGangZoneData, void, setLegacyID, int, int);
PROXY(IPlayerGangZoneData, int, toClientID, int);
PROXY(IPlayerGangZoneData, int, fromClientID, int);
PROXY(IPlayerGangZoneData, void, releaseClientID, int);
PROXY(IPlayerGangZoneData, int, reserveClientID);
PROXY(IPlayerGangZoneData, void, setClientID, int, int);

// todo: getEventDispatcher

// include/Server/Components/LegacyConfig
PROXY(ILegacyConfigComponent, StringView, getConfig, StringView);
PROXY(ILegacyConfigComponent, StringView, getLegacy, StringView);

// include/Server/Components/Menus
PROXY(IMenu, void, setColumnHeader, StringView, MenuColumn);
PROXY(IMenu, int, addCell, StringView, MenuColumn);
PROXY(IMenu, void, disableRow, MenuRow);
PROXY(IMenu, bool, isRowEnabled, MenuRow);
PROXY(IMenu, void, disable);
PROXY(IMenu, bool, isEnabled);
PROXY(IMenu, Vector2,  getPosition);
PROXY(IMenu, int, getRowCount, MenuColumn);
PROXY(IMenu, int, getColumnCount);
PROXY(IMenu, Vector2, getColumnWidths);
PROXY(IMenu, StringView, getColumnHeader, MenuColumn);
PROXY(IMenu, StringView, getCell, MenuColumn, MenuRow);
PROXY(IMenu, void, initForPlayer, IPlayer&);
PROXY(IMenu, void, showForPlayer, IPlayer&);
PROXY(IMenu, void, hideForPlayer, IPlayer&);

PROXY(IPlayerMenuData, uint8_t, getMenuID);
PROXY(IPlayerMenuData, void, setMenuID, uint8_t);

PROXY(IMenusComponent, IMenu*, create, StringView, Vector2, uint8_t, float, float);

// todo: getEventDispatcher

// include/Server/Components/Objects
PROXY(IBaseObject, void, setDrawDistance, float);
PROXY(IBaseObject, float, getDrawDistance);
PROXY(IBaseObject, void, setModel, int);
PROXY(IBaseObject, int, getModel);
PROXY(IBaseObject, void, setCameraCollision, bool);
PROXY(IBaseObject, bool, getCameraCollision);
PROXY(IBaseObject, void, move, ObjectMoveData&);
PROXY(IBaseObject, bool, isMoving);
PROXY(IBaseObject, void, stop);
PROXY(IBaseObject, const ObjectMoveData&, getMovingData);
PROXY(IBaseObject, void, attachToVehicle, IVehicle&, Vector3, Vector3);
PROXY(IBaseObject, void, resetAttachment);
PROXY(IBaseObject, const ObjectAttachmentData&, getAttachmentData);
PROXY(IBaseObject, bool, getMaterialData, uint32_t, const ObjectMaterialData*&);
PROXY(IBaseObject, void, setMaterial, uint32_t, int, StringView, StringView, Colour);
PROXY(IBaseObject, void, setMaterialText, uint32_t, StringView, ObjectMaterialSize, StringView, int, bool, Colour, Colour, ObjectMaterialTextAlign);

PROXY(IObject, void, attachToPlayer, IPlayer&, Vector3, Vector3);
PROXY(IObject, void, attachToObject, IObject&, Vector3, Vector3, bool);

PROXY(IPlayerObject, void, attachToObject, IPlayerObject&, Vector3, Vector3);
PROXY(IPlayerObject, void, attachToPlayer, IPlayer&, Vector3, Vector3);

PROXY(IObjectsComponent, void, setDefaultCameraCollision, bool);
PROXY(IObjectsComponent, bool, getDefaultCameraCollision);
PROXY(IObjectsComponent, IObject*, create, int, Vector3, Vector3, float);

// todo: getEventDispatcher

PROXY(IPlayerObjectData, IPlayerObject*, create, int, Vector3, Vector3, float);
PROXY(IPlayerObjectData, void, setAttachedObject, int, ObjectAttachmentSlotData&);
PROXY(IPlayerObjectData, void, removeAttachedObject, int);
PROXY(IPlayerObjectData, bool, hasAttachedObject, int);
PROXY(IPlayerObjectData, const ObjectAttachmentSlotData&, getAttachedObject, int);
PROXY(IPlayerObjectData, void, beginSelecting);
PROXY(IPlayerObjectData, bool, selectingObject);
PROXY(IPlayerObjectData, void, endEditing);
PROXY(IPlayerObjectData, void, beginEditing, IObject&);
PROXY_OVERLOAD(IPlayerObjectData, void, beginEditing, _player, IPlayerObject&);
PROXY(IPlayerObjectData, bool, editingObject);
PROXY(IPlayerObjectData, void, editAttachedObject, int);

// include/Server/Components/Pawn
// @skip

// include/Server/Components/Pickups

PROXY(IBasePickup, void, setType, PickupType, bool);
PROXY(IBasePickup, PickupType, getType);
PROXY(IBasePickup, void, setPositionNoUpdate, Vector3);
PROXY(IBasePickup, void, setModel, int, bool);
PROXY(IBasePickup, int, getModel);
PROXY(IBasePickup, bool, isStreamedInForPlayer, const IPlayer&);
PROXY(IBasePickup, void, streamInForPlayer, IPlayer&);
PROXY(IBasePickup, void, streamOutForPlayer, IPlayer&);
PROXY(IBasePickup, void, setPickupHiddenForPlayer, IPlayer&, bool);
PROXY(IBasePickup, bool, isPickupHiddenForPlayer, IPlayer&);
PROXY(IBasePickup, void, setLegacyPlayer, IPlayer*);
PROXY(IBasePickup, IPlayer*, getLegacyPlayer);

PROXY(IPickupsComponent, IPickup*, create, int, PickupType, Vector3, uint32_t, bool);
PROXY(IPickupsComponent, int, toLegacyID, int);
PROXY(IPickupsComponent, int, fromLegacyID, int);
PROXY(IPickupsComponent, void, releaseLegacyID, int);
PROXY(IPickupsComponent, int, reserveLegacyID);
PROXY(IPickupsComponent, void, setLegacyID, int, int);
// todo: getEventDispatcher

PROXY(IPlayerPickupData, int, toLegacyID, int);
PROXY(IPlayerPickupData, int, fromLegacyID, int);
PROXY(IPlayerPickupData, void, releaseLegacyID, int);
PROXY(IPlayerPickupData, int, reserveLegacyID);
PROXY(IPlayerPickupData, void, setLegacyID, int, int);
PROXY(IPlayerPickupData, int, toClientID, int);
PROXY(IPlayerPickupData, int, fromClientID, int);
PROXY(IPlayerPickupData, void, releaseClientID, int);
PROXY(IPlayerPickupData, int, reserveClientID);
PROXY(IPlayerPickupData, void, setClientID, int, int);

// include/Server/Components/Recordings
PROXY(IPlayerRecordingData, void, start, PlayerRecordingType, StringView);
PROXY(IPlayerRecordingData, void, stop);

// include/Server/Components/TextDraws
PROXY(ITextDrawBase, Vector2, getPosition);
PROXY(ITextDrawBase, ITextDrawBase&, setPosition, Vector2);
PROXY(ITextDrawBase, void, setText, StringView);
PROXY(ITextDrawBase, StringView, getText);
PROXY(ITextDrawBase, ITextDrawBase&, setLetterSize, Vector2);
PROXY(ITextDrawBase, Vector2, getLetterSize);
PROXY(ITextDrawBase, ITextDrawBase&, setTextSize, Vector2);
PROXY(ITextDrawBase, Vector2, getTextSize);
PROXY(ITextDrawBase, ITextDrawBase&, setAlignment, TextDrawAlignmentTypes);
PROXY(ITextDrawBase, TextDrawAlignmentTypes, getAlignment);
PROXY(ITextDrawBase, ITextDrawBase&, setColour, Colour);
PROXY(ITextDrawBase, Colour, getLetterColour);
PROXY(ITextDrawBase, ITextDrawBase&, useBox, bool);
PROXY(ITextDrawBase, bool, hasBox);
PROXY(ITextDrawBase, ITextDrawBase&, setBoxColour, Colour);
PROXY(ITextDrawBase, Colour, getBoxColour);
PROXY(ITextDrawBase, ITextDrawBase&, setShadow, int);
PROXY(ITextDrawBase, int, getShadow);
PROXY(ITextDrawBase, ITextDrawBase&, setOutline, int);
PROXY(ITextDrawBase, int, getOutline);
PROXY(ITextDrawBase, ITextDrawBase&, setBackgroundColour, Colour);
PROXY(ITextDrawBase, Colour, getBackgroundColour);
PROXY(ITextDrawBase, ITextDrawBase&, setStyle, TextDrawStyle);
PROXY(ITextDrawBase, TextDrawStyle, getStyle);
PROXY(ITextDrawBase, ITextDrawBase&, setProportional, bool);
PROXY(ITextDrawBase, bool, isProportional);
PROXY(ITextDrawBase, ITextDrawBase&, setSelectable, bool);
PROXY(ITextDrawBase, bool, isSelectable);
PROXY(ITextDrawBase, ITextDrawBase&, setPreviewModel, int);
PROXY(ITextDrawBase, int, getPreviewModel);
PROXY(ITextDrawBase, ITextDrawBase&, setPreviewRotation, Vector3);
PROXY(ITextDrawBase, Vector3, getPreviewRotation);
PROXY(ITextDrawBase, ITextDrawBase&, setPreviewVehicleColour, int, int);
PROXY(ITextDrawBase, IntPair, getPreviewVehicleColour);
PROXY(ITextDrawBase, ITextDrawBase&, setPreviewZoom, float);
PROXY(ITextDrawBase, float, getPreviewZoom);
PROXY(ITextDrawBase, void, restream);

PROXY(ITextDraw, void, showForPlayer, IPlayer&);
PROXY(ITextDraw, void, hideForPlayer, IPlayer&);
PROXY(ITextDraw, bool, isShownForPlayer, const IPlayer&);
PROXY(ITextDraw, void, setTextForPlayer, IPlayer&, StringView);

PROXY(IPlayerTextDraw, void, show);
PROXY(IPlayerTextDraw, void, hide);
PROXY(IPlayerTextDraw, bool, isShown);

PROXY(ITextDrawsComponent, ITextDraw*, create, Vector2, StringView);
PROXY_OVERLOAD(ITextDrawsComponent, ITextDraw*, create, _model, Vector2, int);
// todo: getEventDispatcher

PROXY(IPlayerTextDrawData, void, beginSelection, Colour);
PROXY(IPlayerTextDrawData, bool, isSelecting);
PROXY(IPlayerTextDrawData, void, endSelection);
PROXY(IPlayerTextDrawData, IPlayerTextDraw*, create, Vector2, StringView);
PROXY_OVERLOAD(IPlayerTextDrawData, IPlayerTextDraw*, create, _model, Vector2, int);

// include/Server/Components/TextLabels
PROXY(ITextLabelBase, void, setText, StringView);
PROXY(ITextLabelBase, StringView, getText);
PROXY(ITextLabelBase, void, setColour, Colour);
PROXY(ITextLabelBase, Colour, getColour);
PROXY(ITextLabelBase, void, setDrawDistance, float);
PROXY(ITextLabelBase, float, getDrawDistance);
PROXY(ITextLabelBase, void, attachToPlayer, IPlayer&, Vector3);
PROXY(ITextLabelBase, void, attachToVehicle, IVehicle&, Vector3);
PROXY(ITextLabelBase, const TextLabelAttachmentData&, getAttachmentData);
PROXY(ITextLabelBase, void, detachFromPlayer, Vector3);
PROXY(ITextLabelBase, void, detachFromVehicle, Vector3);
PROXY(ITextLabelBase, void, setTestLOS, bool);
PROXY(ITextLabelBase, bool, getTestLOS);
PROXY(ITextLabelBase, void, setColourAndText, Colour, StringView);

PROXY(ITextLabel, bool, isStreamedInForPlayer, IPlayer&);
PROXY(ITextLabel, void, streamInForPlayer, IPlayer&);
PROXY(ITextLabel, void, streamOutForPlayer, IPlayer&);

PROXY(ITextLabelsComponent, ITextLabel*, create, StringView, Colour, Vector3, float, int, bool);
PROXY_OVERLOAD(ITextLabelsComponent, ITextLabel*, create, _player, StringView, Colour, Vector3, float, int, bool, IPlayer&);
PROXY_OVERLOAD(ITextLabelsComponent, ITextLabel*, create, _vehicle, StringView, Colour, Vector3, float, int, bool, IVehicle&);

PROXY(IPlayerTextLabelData, IPlayerTextLabel*, create, StringView, Colour, Vector3, float, bool);
PROXY_OVERLOAD(IPlayerTextLabelData, IPlayerTextLabel*, create, _player, StringView, Colour, Vector3, float, bool, IPlayer&);
PROXY_OVERLOAD(IPlayerTextLabelData, IPlayerTextLabel*, create, _vehicle, StringView, Colour, Vector3, float, bool, IVehicle&);

// include/Server/Components/Timers
// @skip
	
// include/Server/Components/Unicode
// @skip
	
// include/Server/Components/Variables
// @skip

// include/Server/Components/Vehicles

PROXY(IVehicle, void, setSpawnData, VehicleSpawnData&);
PROXY(IVehicle, VehicleSpawnData, getSpawnData);
PROXY(IVehicle, bool, isStreamedInForPlayer, IPlayer&);
PROXY(IVehicle, void, streamInForPlayer, IPlayer&);
PROXY(IVehicle, void, streamOutForPlayer, IPlayer&);
PROXY(IVehicle, void, setColour, int, int);
PROXY(IVehicle, IntPair, getColour);
PROXY(IVehicle, void, setHealth, float);
PROXY(IVehicle, float, getHealth);
PROXY(IVehicle, bool, updateFromDriverSync, VehicleDriverSyncPacket&, IPlayer&);
PROXY(IVehicle, bool, updateFromPassengerSync, VehiclePassengerSyncPacket&, IPlayer&);
PROXY(IVehicle, bool, updateFromUnoccupied, VehicleUnoccupiedSyncPacket&, IPlayer&);
PROXY(IVehicle, bool, updateFromTrailerSync, VehicleTrailerSyncPacket&, IPlayer&);
PROXY(IVehicle, const FlatPtrHashSet<IPlayer>&, streamedForPlayers);
PROXY(IVehicle, IPlayer*, getDriver);
PROXY(IVehicle, const FlatHashSet<IPlayer*>&, getPassengers);
PROXY(IVehicle, void, setPlate, StringView);
PROXY(IVehicle, StringView, getPlate);
PROXY(IVehicle, void, setDamageStatus, int, int, uint8_t, uint8_t, IPlayer*);
PROXY(IVehicle, void, getDamageStatus, int&, int&, int&, int&);
PROXY(IVehicle, void, setPaintJob, int);
PROXY(IVehicle, int, getPaintJob);
PROXY(IVehicle, void, addComponent, int);
PROXY(IVehicle, int, getComponentInSlot, int);
PROXY(IVehicle, void, removeComponent, int);
PROXY(IVehicle, void, putPlayer, IPlayer&, int);
PROXY(IVehicle, void, setZAngle, float);
PROXY(IVehicle, float, getZAngle);
PROXY(IVehicle, void, setParams, VehicleParams&);
PROXY(IVehicle, void, setParamsForPlayer, IPlayer&, VehicleParams&);
PROXY(IVehicle, VehicleParams, getParams);
PROXY(IVehicle, bool, isDead);
PROXY(IVehicle, void, respawn);
PROXY(IVehicle, Seconds, getRespawnDelay);
PROXY(IVehicle, void, setRespawnDelay, Seconds);
PROXY(IVehicle, bool, isRespawning);
PROXY(IVehicle, void, setInterior, int);
PROXY(IVehicle, int, getInterior);
PROXY(IVehicle, void, attachTrailer, IVehicle&);
PROXY(IVehicle, void, detachTrailer);
PROXY(IVehicle, bool, isTrailer);
PROXY(IVehicle, IVehicle*, getTrailer);
PROXY(IVehicle, IVehicle*, getCab);
PROXY(IVehicle, void, repair);
PROXY(IVehicle, void, addCarriage, IVehicle*, int);
PROXY(IVehicle, void, updateCarriage, Vector3, Vector3);
PROXY(IVehicle, const CarriagesArray&, getCarriages);
PROXY(IVehicle, void, setVelocity, Vector3);
PROXY(IVehicle, Vector3, getVelocity);
PROXY(IVehicle, void, setAngularVelocity, Vector3);
PROXY(IVehicle, Vector3, getAngularVelocity);
PROXY(IVehicle, int, getModel);
PROXY(IVehicle, uint8_t, getLandingGearState);
PROXY(IVehicle, bool, hasBeenOccupied);
PROXY(IVehicle, const TimePoint&, getLastOccupiedTime);
PROXY(IVehicle, const TimePoint&, getLastSpawnTime);
PROXY(IVehicle, bool, isOccupied);
PROXY(IVehicle, void, setSiren, bool);
PROXY(IVehicle, uint8_t, getSirenState);
PROXY(IVehicle, uint32_t, getHydraThrustAngle);
PROXY(IVehicle, float, getTrainSpeed);
PROXY(IVehicle, int, getLastDriverPoolID);

PROXY(IVehiclesComponent, VehicleModelArray&, models);
PROXY(IVehiclesComponent, IVehicle*, create, bool, int, Vector3, float, int, int, Seconds, bool);

PROXY(IPlayerVehicleData, IVehicle*, getVehicle);
PROXY(IPlayerVehicleData, void, resetVehicle);
PROXY(IPlayerVehicleData, int, getSeat);
PROXY(IPlayerVehicleData, bool, isInModShop);
PROXY(IPlayerVehicleData, bool, isInDriveByMode);
PROXY(IPlayerVehicleData, bool, isCuffed);

// include/component
PROXY(IExtensible, IExtension*, getExtension, UID);

PROXY(IComponent, int, supportedVersion);
PROXY(IComponent, StringView, componentName);

PROXY(IComponentList, IComponent*, queryComponent, UID);

// include/core
PROXY(IConfig, StringView, getString, StringView);
PROXY(IConfig, int*, getInt, StringView);
PROXY(IConfig, float*, getFloat, StringView);
PROXY(IConfig, size_t, getStrings, StringView, Span<StringView>);
PROXY(IConfig, size_t, getStringsCount, StringView);
PROXY(IConfig, ConfigOptionType, getType, StringView);
PROXY(IConfig, size_t, getBansCount);
PROXY(IConfig, const BanEntry&, getBan, size_t);
PROXY(IConfig, void, addBan, BanEntry&);
PROXY_OVERLOAD(IConfig, void, removeBan, _index, size_t);
PROXY(IConfig, void, removeBan, BanEntry&);
PROXY(IConfig, void, writeBans);
PROXY(IConfig, void, reloadBans);
PROXY(IConfig, void, clearBans);
PROXY(IConfig, bool, isBanned, BanEntry&);
PROXY(IConfig, BoolStringPair, getNameFromAlias, StringView);
PROXY(IConfig, void, enumOptions, OptionEnumeratorCallback&);
PROXY(IConfig, bool*, getBool, StringView);

// @skip: ILogger due to varargs; need to write a wrapper w/a vararg

PROXY(ICore, SemanticVersion, getVersion);
PROXY(ICore, int, getNetworkBitStreamVersion);
PROXY(ICore, IPlayerPool&, getPlayers);
PROXY(ICore, IConfig&, getConfig);
PROXY(ICore, const FlatPtrHashSet<INetwork>&, getNetworks);
PROXY(ICore, unsigned, getTickCount);
PROXY(ICore, void, setGravity, float);
PROXY(ICore, float, getGravity);
PROXY(ICore, void, setWeather, int);
PROXY(ICore, void, setWorldTime, Hours);
PROXY(ICore, void, useStuntBonuses, bool);
PROXY(ICore, void, setData, SettableCoreDataType, StringView);
PROXY(ICore, void, setThreadSleep, Microseconds);
PROXY(ICore, void, useDynTicks, const bool);
PROXY(ICore, void, resetAll);
PROXY(ICore, void, reloadAll);
PROXY(ICore, StringView, getWeaponName, PlayerWeapon);
PROXY(ICore, void, connectBot, StringView, StringView);
PROXY(ICore, unsigned, tickRate);
PROXY(ICore, StringView, getVersionHash);

// todo: getEventDispatcher

// include/entity
PROXY(IIDProvider, int, getID);

PROXY(IEntity, Vector3, getPosition);
PROXY(IEntity, void, setPosition, Vector3);
PROXY(IEntity, GTAQuat, getRotation);
PROXY(IEntity, void, setRotation, GTAQuat);
PROXY(IEntity, int, getVirtualWorld);
PROXY(IEntity, void, setVirtualWorld, int);

// @skip include/network

// include/player
PROXY(IPlayer, void, kick);
PROXY(IPlayer, void, ban, StringView);
PROXY(IPlayer, bool, isBot);
PROXY(IPlayer, PeerNetworkData,  getNetworkData);
PROXY(IPlayer, unsigned, getPing, ) ;
PROXY(IPlayer, bool, sendPacket, Span<uint8_t>, int, bool);
PROXY(IPlayer, bool, sendRPC, int, Span<uint8_t>, int, bool);
PROXY(IPlayer, void, broadcastRPCToStreamed, int, Span<uint8_t>, int, bool);
PROXY(IPlayer, void, broadcastPacketToStreamed, Span<uint8_t>, int, bool);
PROXY(IPlayer, void, broadcastSyncPacket, Span<uint8_t>, int);
PROXY(IPlayer, void, spawn);
PROXY(IPlayer, ClientVersion, getClientVersion);
PROXY(IPlayer, StringView, getClientVersionName);
PROXY(IPlayer, void, setPositionFindZ, Vector3);
PROXY(IPlayer, void, setCameraPosition, Vector3);
PROXY(IPlayer, Vector3, getCameraPosition);
PROXY(IPlayer, void, setCameraLookAt, Vector3, int);
PROXY(IPlayer, Vector3, getCameraLookAt);
PROXY(IPlayer, void, setCameraBehind);
PROXY(IPlayer, void, interpolateCameraPosition, Vector3, Vector3, int, PlayerCameraCutType);
PROXY(IPlayer, void, interpolateCameraLookAt, Vector3, Vector3, int, PlayerCameraCutType);
PROXY(IPlayer, void, attachCameraToObject, IObject&);
PROXY_OVERLOAD(IPlayer, void, attachCameraToObject, _player, IPlayerObject&);
PROXY(IPlayer, EPlayerNameStatus, setName, StringView);
PROXY(IPlayer, StringView, getName);
PROXY(IPlayer, StringView, getSerial);
PROXY(IPlayer, void, giveWeapon, WeaponSlotData);
PROXY(IPlayer, void, removeWeapon, uint8_t);
PROXY(IPlayer, void, setWeaponAmmo, WeaponSlotData);
PROXY(IPlayer, WeaponSlots, getWeapons);
PROXY(IPlayer, WeaponSlotData, getWeaponSlot, int);
PROXY(IPlayer, void, resetWeapons);
PROXY(IPlayer, void, setArmedWeapon, uint32_t);
PROXY(IPlayer, uint32_t, getArmedWeapon);
PROXY(IPlayer, uint32_t, getArmedWeaponAmmo);
PROXY(IPlayer, void, setShopName, StringView);
PROXY(IPlayer, StringView, getShopName);
PROXY(IPlayer, void, setDrunkLevel, int);
PROXY(IPlayer, int, getDrunkLevel);
PROXY(IPlayer, void, setColour, Colour);
PROXY(IPlayer, Colour,  getColour);
PROXY(IPlayer, void, setOtherColour, IPlayer&, Colour);
PROXY(IPlayer, bool, getOtherColour, IPlayer&, Colour&);
PROXY(IPlayer, void, setControllable, bool);
PROXY(IPlayer, bool, getControllable);
PROXY(IPlayer, void, setSpectating, bool);
PROXY(IPlayer, void, setWantedLevel, unsigned);
PROXY(IPlayer, unsigned, getWantedLevel);
PROXY(IPlayer, void, playSound, uint32_t, Vector3);
PROXY(IPlayer, uint32_t, lastPlayedSound);
PROXY(IPlayer, void, playAudio, StringView, bool, Vector3, float);
PROXY(IPlayer, bool, playerCrimeReport, IPlayer&, int);
PROXY(IPlayer, void, stopAudio);
PROXY(IPlayer, StringView, lastPlayedAudio);
PROXY(IPlayer, void, createExplosion, Vector3, int, float);
PROXY(IPlayer, void, sendDeathMessage, IPlayer&, IPlayer*, int);
PROXY(IPlayer, void, sendEmptyDeathMessage);
PROXY(IPlayer, void, removeDefaultObjects, unsigned, Vector3, float);
PROXY(IPlayer, void, forceClassSelection);
PROXY(IPlayer, void, setMoney, int);
PROXY(IPlayer, void, giveMoney, int);
PROXY(IPlayer, void, resetMoney);
PROXY(IPlayer, int, getMoney);
PROXY(IPlayer, void, setMapIcon, int, Vector3, int, Colour, MapIconStyle);
PROXY(IPlayer, void, unsetMapIcon, int);
PROXY(IPlayer, void, useStuntBonuses, bool);
PROXY(IPlayer, void, toggleOtherNameTag, IPlayer&, bool);
PROXY(IPlayer, void, setTime, Hours, Minutes);
PROXY(IPlayer, HoursMinutesPair, getTime);
PROXY(IPlayer, void, useClock, bool);
PROXY(IPlayer, bool, hasClock);
PROXY(IPlayer, void, useWidescreen, bool);
PROXY(IPlayer, bool, hasWidescreen);
PROXY(IPlayer, void, setTransform, GTAQuat);
PROXY(IPlayer, void, setHealth, float);
PROXY(IPlayer, float, getHealth);
PROXY(IPlayer, void, setScore, int);
PROXY(IPlayer, int, getScore);
PROXY(IPlayer, void, setArmour, float);
PROXY(IPlayer, float, getArmour);
PROXY(IPlayer, void, setGravity, float);
PROXY(IPlayer, float, getGravity);
PROXY(IPlayer, void, setWorldTime, Hours);
PROXY(IPlayer, void, applyAnimation, const AnimationData&, PlayerAnimationSyncType);
PROXY(IPlayer, void, clearAnimations, PlayerAnimationSyncType);
PROXY(IPlayer, PlayerAnimationData, getAnimationData);
PROXY(IPlayer, PlayerSurfingData, getSurfingData);
PROXY(IPlayer, void, streamInForPlayer, IPlayer&);
PROXY(IPlayer, bool, isStreamedInForPlayer, const IPlayer&);
PROXY(IPlayer, void, streamOutForPlayer, IPlayer&);
PROXY(IPlayer, const FlatPtrHashSet<IPlayer>&, streamedForPlayers);
PROXY(IPlayer, PlayerState, getState);
PROXY(IPlayer, void, setTeam, int);
PROXY(IPlayer, int, getTeam);
PROXY(IPlayer, void, setSkin, int, bool);
PROXY(IPlayer, int, getSkin);
PROXY(IPlayer, void, setChatBubble, StringView, const Colour&, float, Milliseconds);
PROXY(IPlayer, void, sendClientMessage, const Colour&, StringView);
PROXY(IPlayer, void, sendChatMessage, IPlayer&, StringView);
PROXY(IPlayer, void, sendCommand, StringView);
PROXY(IPlayer, void, sendGameText, StringView, Milliseconds, int);
PROXY(IPlayer, void, hideGameText, int);
PROXY(IPlayer, bool, hasGameText, int);
PROXY(IPlayer, bool, getGameText, int, StringView&, Milliseconds&, Milliseconds&);
PROXY(IPlayer, void, setWeather, int);
PROXY(IPlayer, int, getWeather);
PROXY(IPlayer, void, setWorldBounds, Vector4);
PROXY(IPlayer, Vector4, getWorldBounds);
PROXY(IPlayer, void, setFightingStyle, PlayerFightingStyle);
PROXY(IPlayer, PlayerFightingStyle, getFightingStyle);
PROXY(IPlayer, void, setSkillLevel, PlayerWeaponSkill, int);
PROXY(IPlayer, void, setAction, PlayerSpecialAction);
PROXY(IPlayer, PlayerSpecialAction, getAction);
PROXY(IPlayer, void, setVelocity, Vector3);
PROXY(IPlayer, Vector3, getVelocity);
PROXY(IPlayer, void, setInterior, unsigned);
PROXY(IPlayer, unsigned, getInterior);
PROXY(IPlayer, PlayerKeyData,  getKeyData);
PROXY(IPlayer, const SkillsArray&, getSkillLevels);
PROXY(IPlayer, PlayerAimData,  getAimData);
PROXY(IPlayer, PlayerBulletData,  getBulletData);
PROXY(IPlayer, void, useCameraTargeting, bool);
PROXY(IPlayer, bool, hasCameraTargeting);
PROXY(IPlayer, void, removeFromVehicle, bool);
PROXY(IPlayer, IPlayer*, getCameraTargetPlayer);
PROXY(IPlayer, IVehicle*, getCameraTargetVehicle);
PROXY(IPlayer, IObject*, getCameraTargetObject);
PROXY(IPlayer, IActor*, getCameraTargetActor);
PROXY(IPlayer, IPlayer*, getTargetPlayer);
PROXY(IPlayer, IActor*, getTargetActor);
PROXY(IPlayer, void, setRemoteVehicleCollisions, bool);
PROXY(IPlayer, void, spectatePlayer, IPlayer&, PlayerSpectateMode);
PROXY(IPlayer, void, spectateVehicle, IVehicle&, PlayerSpectateMode);
PROXY(IPlayer, PlayerSpectateData,  getSpectateData);
PROXY(IPlayer, void, sendClientCheck, int, int, int, int);
PROXY(IPlayer, void, toggleGhostMode, bool);
PROXY(IPlayer, bool, isGhostModeEnabled);
PROXY(IPlayer, int, getDefaultObjectsRemoved);
PROXY(IPlayer, bool, getKickStatus);
PROXY(IPlayer, void, clearTasks, PlayerAnimationSyncType);
PROXY(IPlayer, void, allowWeapons, bool);
PROXY(IPlayer, bool, areWeaponsAllowed);
PROXY(IPlayer, void, allowTeleport, bool);
PROXY(IPlayer, bool, isTeleportAllowed);
PROXY(IPlayer, bool, isUsingOfficialClient);

PROXY(IPlayerPool, const FlatPtrHashSet<IPlayer>&, entries);
PROXY(IPlayerPool, const FlatPtrHashSet<IPlayer>&, players);
PROXY(IPlayerPool, const FlatPtrHashSet<IPlayer>&, bots);
PROXY(IPlayerPool, bool, isNameTaken, StringView, const IPlayer*);
PROXY(IPlayerPool, void, sendClientMessageToAll, const Colour&, StringView);
PROXY(IPlayerPool, void, sendChatMessageToAll, IPlayer&, StringView);
PROXY(IPlayerPool, void, sendGameTextToAll, StringView, Milliseconds, int);
PROXY(IPlayerPool, void, hideGameTextForAll, int);
PROXY(IPlayerPool, void, sendDeathMessageToAll, IPlayer*, IPlayer&, int);
PROXY(IPlayerPool, void, sendEmptyDeathMessageToAll);
PROXY(IPlayerPool, void, createExplosionForAll, Vector3, int, float);
PROXY(IPlayerPool, NewConnectionPlayerPair, requestPlayer, const PeerNetworkData&, const PeerRequestParams&);
PROXY(IPlayerPool, void, broadcastPacket, Span<uint8_t>, int, const IPlayer*, bool);
PROXY(IPlayerPool, void, broadcastRPC, int, Span<uint8_t>, int, const IPlayer*, bool);
PROXY(IPlayerPool, bool, isNameValid, StringView);
PROXY(IPlayerPool, void, allowNickNameCharacter, char, bool);
PROXY(IPlayerPool, bool, isNickNameCharacterAllowed, char);
PROXY(IPlayerPool, Colour, getDefaultColour, int);

// todo: getXDispatcher


#pragma clang diagnostic pop
