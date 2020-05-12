import numpy as np

import mmap
import threading

import cv2
import time
import argparse
import io
import gzip
import os
import tqdm

length_idhw = 3 * 4
length_skel = 17 * 4 * 3
length_frame = 640 * 480 * 3
length_total = length_idhw + length_skel + length_frame

def record_from_mem():
    # Returns a massive flattened byte array. We know the length of each frame
    # so we don't need to split it!
    recording = io.BytesIO()
    update_id = -9 # Something we're unlikely to start on
    nframes = np.uint32(0)
    recording.seek(0)
    recording.write(nframes.tobytes()) # Placeholder
    with mmap.mmap(-1, length_total, tagname="jpysync") as data:
        input('Press any key to start recording...')
        print("Recording...When the stream stops (Q on recorder window), recording will stop automatically.")
        while update_id != -10:
            # -10 is the stop id
            data.flush()
            data.seek(0)
            update_buf = data.read(4)
            read_id = int(np.frombuffer(update_buf, dtype=np.int32)[0])
            if read_id != update_id:
                data_buf = data.read()
                recording.write(update_buf)
                recording.write(data_buf)
                update_id = read_id
                nframes += 1
    recording.seek(0)
    recording.write(nframes.tobytes()) # Replace placeholder
    recording.seek(0)
    return recording.read(), int(nframes)

def playback_from_mem(mem, fps):
    mem.seek(0)
    has_reached_end = False
    nframes = int(np.frombuffer(mem.read(4), dtype=np.uint32)[0])
    nreadframes = 0
    if fps > 0:
        spf = 1.0 / fps
    frames = []
    while not has_reached_end:
        update_buf = mem.read(4)
        frame_buf = mem.read(length_total - 4)
        if len(update_buf) + len(frame_buf) < length_total:
            has_reached_end = True
            break
        else:
            frames.append((update_buf, frame_buf))
            nreadframes += 1
    if nreadframes != nframes:
        print("Warning: Number of read frames was not equal to the number stated in file.")
    with mmap.mmap(-1, length_total, tagname="jpysync") as data:
        should_loop = True
        while should_loop:
            # We wait for input AFTER opening the file, so the program can connect to this memory
            input("Ready to play %d frames... Press any key to start!" % (nreadframes,))
            # Now we have all the frames
            for (update_buf, frame_buf) in tqdm.tqdm(frames, desc="Playing"):
                # We have the full buffer
                data.flush()
                data.seek(4)
                data.write(frame_buf)
                data.seek(0)
                data.write(update_buf)
                data.seek(0)
                data.flush()
                # Wait some amount of time in between. This is not super accurate
                if fps > 0:
                    time.sleep(spf)
            should_loop = 'y' in input("Loop? (Y/N)").lower()
        

    

def record(fn):
    recorded_bytes, nframes = record_from_mem()
    print("Stream stopped. Saving to file...")
    with gzip.open(fn, 'wb') as f:
        f.write(recorded_bytes)
    print("Saved %d frames to %s" % (nframes, os.path.relpath(fn)))

def play(fn, fps=30):
    print("Uncompressing & Loading from %s" % (os.path.relpath(fn), ))
    with gzip.open(fn, 'rb') as f:
        recorded_bytes = f.read()
    stream = io.BytesIO()
    stream.write(recorded_bytes)
    stream.seek(0)
    playback_from_mem(stream, fps)
    stream.close()
    print("Done playback!")


def recording_name(fn):
    return '%s.zpose' % (os.path.splitext(os.path.abspath(fn))[0], )


def parse_args():
    parser = argparse.ArgumentParser(description="Records or plays back a pose-video")
    parser.add_argument('mode', choices=['play', 'record'], help="Mode, playback or record")
    parser.add_argument('file', type=recording_name, help = "file to write to")
    parser.add_argument('--fps', default=30, type=int, help="FPS to playback at (default: 30)")
    return parser.parse_args()


def main():
    args = parse_args()
    if args.mode == 'play':
        play(args.file)
    else:
        record(args.file)


if __name__ == '__main__':
    main()
