/// The minimal game runner: drives the CE program in MinimalGame.Image on a
/// Jetpac2 port Machine. Fable-clean: no I/O, no GUI - shells and tests
/// share this module.
module MinimalGame.Game

open Jetpac2.Core

/// The per-frame CE driver (69888 T-states). CE ops dispatch by PC from the
/// program index; raw blocks and anything outside the CE layout fall through
/// to the interpreter. Returns (frameStart, frameEnd) for audio.
let runFrame (m: Machine) : int64 * int64 =
    MinimalGame.Image.index.Attach m
    Z80.runFrame MinimalGame.Image.index m

/// Entry state: the image at 0x8000, PC=0x8000, SP=0xFFFE, interrupts
/// off. Pure (no I/O), so shells and tests share it.
let entryState (image: byte[]) : byte[] * string =
    let mem = Array.zeroCreate<byte> 0x10000
    let len = min image.Length (0x10000 - 0x8000)
    Array.blit image 0 mem 0x8000 len

    let state =
        "af=0000\nbc=0000\nde=0000\nhl=0000\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=0000\niy=0000\nsp=FFFE\npc=8000\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n"

    mem, state
