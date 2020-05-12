from . import server, recorder, viewer
import argparse


def parse_args():
    parser = argparse.ArgumentParser(description="Runs basic UniPose scripts")
    parser.add_argument("command", choices=["server", "record", "play", "view"], help="\nSERVER mode - Start a unipose server.\nRECORD mode - record the pose frame stream from the current unipose server, for playback later\nPLAY mode - play a recorded posestream\nVIEWER mode - View the currently live pose stream")
    parser.add_argument("-c", "--cam_id", default=0, help="If SERVER mode, the camera/video file to use as input (default 0)")
    parser.add_argument('-f', "--file", default=None, type=recorder.recording_name, help="If RECORD / PLAY mode, the file to record to / play from")
    parser.add_argument('--cpu', action='store_true', default=False, help='If SERVER mode, force the model to run on CPU. Default: auto-detect. HIGHLY RECOMMEND AGAINST THIS!')
    parser.add_argument('--draw', action='store_true', default=False, help='If SERVER mode, draw the webcam feed (mirrored) & pose. This slows the server, so is disabled by default.')
    args = parser.parse_args()
    if args.command in ["record", "play"] and args.file is None:
        raise Exception("File must be specified if record/play mode")
    return args

def main():
    args = parse_args()
    if args.command == "server":
        server.run_pose_server(args.cam_id, cpu=args.cpu, draw=args.draw, mirror=True)
    elif args.command == "record":
        recorder.record(args.file)
    elif args.command == "play":
        recorder.play(args.file)
    elif args.command == "view":
        viewer.view_stream()

if __name__ == "__main__":
    main()