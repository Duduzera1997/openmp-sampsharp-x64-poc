# openmp-sampsharp-x64-poc
proof-of-concept code. Attempt to run x64 .NET code with an up-to-date well supported runtime as an open.mp component.

Goal
----
The goal of this project is to create a ready-for-production concept that can be easily used to generate a
bridge between .NET and the open.mp component/data structure.

Status
------
The current implementation works but requires several quality improvements and bug fixes. I'm currently
working on improving the source generation to use syntax trees instead of parsed concatenated strings for better
performance and allowing proper marashalling using the existing marshalling data structures available in .NET (see
https://github.com/dotnet/runtime/blob/main/docs/design/libraries/LibraryImportGenerator/UserTypeMarshallingV2.md )

Contents  
--------  
The project consists of several parts:  
1) The open.mp component written in c++ which hosts the dotnet runtime (x64) and provides C declarations for all C++
functions using very simple proxy functions. The component also contains some small C++ class implementations required
for registering event handlers which can be created and deleted using declared C functions. open.mp uses the robin hood
library for hash tables which are exposed in the open.mp API. Therefore we also provide proxy functions for a subset of
functions exposed in its API. The component is currently fully functional on Windows, but lacks Linux support.  
2) The source generator generates various structures. Some source gen attributes are currently post-fixed with a number
to differenntate between a new (2) and old (1) code generator. The old code generator should eventually disappear. The
source generator generates the following code structures:  
   - Implementations of event handler infastructure (registration/unregistration) using the `[OpenMpEventHandler]`
   attribute  
   - P/Invokes + marshalling of functions exposed in the open.mp api using the `[OpenMpApi]` attribute  
   - (TODO) An `OnInit` entry point for the open.mp to invoke. The entry point should have a fixed namespace, class and
   method name and should wire up the communication between the open.mp component and the .NET portion of SampSharp. It
   should also generate a `Main` entry point. This entry point should either be empty or provide a consosle message that
   the game mode should be launched through open.mp. The `Main` entry point is r eqquire in order for .NET to write a
   runtimeconfig which we need to run the gamemode. The entry point generator has not be implemented yet. A manually
   written entry point currrently resides in the `Interop` class.  
4) The analyzer provides useful diagnostics for helping writing proper SampSharp code that interfaces with open.mp. It
should provide warnings/errors when the written code cannot be marshalled or no P/Invoke can be generated.  
5) The SashManaged library (later: SampSharp.OpenMp.Core) provides the open.mp API and data structures in .NET. These
APIs are usable but not as user-friendly as we'd like to see in SampSharp.  
6) (TODO) Lastly we need a user-friendly library which allows the user to easily write game modes in .NET. It will be
based on the ECS library SampSharp.Entities. Since the ECS framework seems more flexible and performant due to the way
events propagate I'll not be rewriting SampSharp.GameMode for open.mp. Maintaining two versions of SampSharp greatly
increases the amount of effort required from maintainers. When the open.mp version of SampSharp is ready, the old
SampSharp will go into maintenance mode and only receive critical bug fixes.  

Benefits  
--------  
There are a number of key differences between the libraries in this repo and the libraries in the main
SampSharp repo: - We use the open.mp API instead of the the Pawn scripting API for communicating between .NET and the
SA-MP server backend. Aside from the big performance benefit this change provides, we also gain an increased flexibility
due to getting access to the entire server API instead of the subset exposed to Pawn.  
- We build x64 binaries instead of x86. While open.mp does not yet ship their x64 server binaries from the releases
page, daily builds are available for x64
(https://github.com/openmultiplayer/open.mp/actions/workflows/build.yml?query=branch%3Amaster). .NET does however not
provide x86 binaries for Linux (they do for Windows). While 3rd party x86 .NET binaries are available, these are not
supported by MS and may have stability issues.  
- The bridge between the .NET API and server API are generated at compile-time instead of at run-time. This means that
the initial performance of the server should be better, it easier to diagnose issues in generated code and easier to
improve the generated code since we generate c# code instead of itermediate code (IL).  
