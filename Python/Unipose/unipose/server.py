# Modified by Julie, based on original "webcam_demo.py"

import torch
import cv2
import time
import argparse
from easydict import EasyDict
import mmap
import numpy as np

from . import posenet
import os

mydir = os.path.dirname(os.path.abspath(__file__))

def parse_camera(s):
    try:
        return int(s)
    except:
        return s


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument('--cam_id', type=parse_camera, default=0)
    parser.add_argument('--draw', action='store_true', default=False)
    parser.add_argument("--mirror", default=False, action="store_true", help="Mirror the image")
    parser.add_argument('--cpu', default=False, action="store_true", help="Force the model to run on CPU")
    args = parser.parse_args()

    # Take away these arguments so it works as expected

    #parser.add_argument('--model', type=int, default=101)
    #parser.add_argument('--cam_width', type=int, default=640)
    #parser.add_argument('--cam_height', type=int, default=480)
    #parser.add_argument('--scale_factor', type=float, default=0.5)
        
    args.cam_width = 640
    args.cam_height = 480
    args.scale_factor = 0.5
    args.model = 101
    return args

class Skeleton():
    CLOSE_SERVER_UPDATE_ID = -10
    def __init__(self, w, h):
        # Unique ID to tell when updates have happened
        self.update_id = np.int32(0)
        self.nkps = len(posenet.constants.PART_NAMES)
        self.kp_names = posenet.constants.PART_NAMES
        self.scores = [0.0] * self.nkps
        self.positions = [(0.0, 0.0)] * self.nkps
        self.width, self.height = (np.int32(w), np.int32(h))
        self.assign_data()
        self.frame = np.zeros((self.height, self.width, 3), dtype=np.uint8)
        self.senddata = np.zeros((3*self.nkps,), dtype=np.float32)
    def update(self, keypoints, scores, minscore = 0.5):
        for i in range(self.nkps):
            #if scores[i] >= minscore:
            self.scores[i] = scores[i]
            self.positions[i] = tuple(keypoints[i])
        self.assign_data()
    def update_image(self, frame):
        # unsigned byte (uint8), type code 'B'
        self.frame = frame
    def assign_data(self):
        senddata = []
        datanames = ['%s%d' % (s, i) for i in range(self.nkps) for s in ['x','y','s']]
        for attrname in datanames:
            t = attrname[0]
            ix = int(attrname[1:])
            if t == 's':
                v = self.scores[ix]
            elif t == 'x':                
                v = self.positions[ix][1]
            elif t == 'y':
                v = self.positions[ix][0]
            senddata.append(v)
        self.senddata = np.array(senddata, dtype=np.float32)
        self.update_id += 1
        if self.update_id == Skeleton.CLOSE_SERVER_UPDATE_ID:
            # Never use the close server update ID normally
            self.update_id += 1
    def to_bytes(self):
        # We'll send update ID, width, height, skeleton points, frame data
        b_update = self.update_id.tobytes()
        b_msg = np.array([self.width, self.height]).tobytes() + self.senddata.tobytes() + self.frame.tobytes()
        return b_update, b_msg
    def write_to_stream(self, stream):
        b_update, b_msg = self.to_bytes()
        stream.flush()
        stream.seek(len(b_update))
        stream.write(b_msg)
        stream.seek(0)
        stream.write(b_update)
        stream.flush()
        stream.seek(0)
    def close(self, stream):
        self.update_id = np.int32(Skeleton.CLOSE_SERVER_UPDATE_ID)
        self.write_to_stream(stream)
    def __len__(self):
        return sum(map(len,self.to_bytes()))
    def types_for_attrs(self):
        return 'iioo', ['width','height', 'senddata','frame']
        
def generate_filename():
    templatefn = os.path.join(mydir,'screenshots', 'image_%05d.png')
    saveimg_num = 1
    while os.path.isfile(templatefn % (saveimg_num,)):
        saveimg_num+= 1
    return templatefn % (saveimg_num,)


def run_pose_server_with_args(args):
    model = posenet.load_model(args.model)

    device = 'cuda' if torch.cuda.is_available() and not args.cpu else 'cpu'

    model = model.to(device)
    output_stride = model.output_stride

    theskel = Skeleton(args.cam_width, args.cam_height)

    mm_file = mmap.mmap(-1, len(theskel), tagname="jpysync")

    cap = cv2.VideoCapture(args.cam_id)
    if not args.draw:
        recording_sprite = cv2.imread(os.path.join(mydir, 'sprites','recording.png'))
        paused_sprite = cv2.imread(os.path.join(mydir, 'sprites','paused.png'))
        cv2.namedWindow('recording', cv2.WINDOW_NORMAL)
        cv2.imshow('recording', recording_sprite)
    cap.set(3, args.cam_width)
    cap.set(4, args.cam_height)
    

    should_continue = True

    start = time.time()
    frame_count = 0
    paused = False
    overlay_image = None
    
    print("Press Q to quit, P to (un)pause, S to take a screenshot")

    while should_continue:
        if not paused:
            input_image, display_image, output_scale = posenet.read_cap(
                cap, scale_factor=args.scale_factor, output_stride=output_stride)

            with torch.no_grad():
                input_image = torch.Tensor(input_image).to(device)

                heatmaps_result, offsets_result, displacement_fwd_result, displacement_bwd_result = model(input_image)

                pose_scores, keypoint_scores, keypoint_coords = posenet.decode_multiple_poses(
                    heatmaps_result.squeeze(0),
                    offsets_result.squeeze(0),
                    displacement_fwd_result.squeeze(0),
                    displacement_bwd_result.squeeze(0),
                    output_stride=output_stride,
                    max_pose_detections=10,
                    min_pose_score=0.15)

            keypoint_coords *= output_scale

            # TODO this isn't particularly fast, use GL for drawing and display someday...
            
            theskel.update(keypoint_coords[0], keypoint_scores[0])
            theskel.update_image(display_image)
            theskel.write_to_stream(mm_file)

            #center = tuple(map(int,keypoint_coords[0][posenet.constants.PART_NAMES.index('nose')]))[::-1]
            #print(keypoint_scores[0][0])
            #cv2.circle(display_image, center, 10, (0,0,255), thickness=10)

            if args.draw:
                overlay_image = posenet.draw_skel_and_kp(
                    display_image, pose_scores, keypoint_scores, keypoint_coords,
                    min_pose_score=0.15, min_part_score=0.1)
                if args.mirror:
                    overlay_image = cv2.flip(overlay_image, 1) # Flip horizontally
                cv2.imshow('posenet', overlay_image)
            
            frame_count += 1
        keys = cv2.waitKey(1) & 0xFF
        if keys == ord('q'):
            should_continue = False
        elif keys == ord('p'):
            paused = not paused
            if not args.draw:
                cv2.imshow('recording', paused_sprite if paused else recording_sprite)
        elif keys == ord('s') and args.draw and overlay_image is not None:
            # Save the image you see
            savepath = generate_filename()
            cv2.imwrite(savepath, overlay_image)
            savepath = generate_filename()
            cv2.imwrite(savepath, display_image)

    print('Average FPS: ', frame_count / (time.time() - start))
    theskel.close(mm_file)
    mm_file.close()

def run_pose_server(cam_id = 0, draw=False, mirror=False, cpu=False):
    args = EasyDict()
    args.cam_id = parse_camera(cam_id)
    args.draw = draw
    args.mirror = mirror
    args.cpu = cpu

    args.model = 101
    args.scale_factor = 0.5
    args.cam_width = 640
    args.cam_height = 480
    run_pose_server_with_args(args)

    

def main():
    args = parse_args()
    run_pose_server_with_args(args)


if __name__ == "__main__":
    main()