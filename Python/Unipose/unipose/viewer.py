import numpy as np

import mmap

import cv2

length_idhw = 3 * 4
length_skel = 17 * 4 * 3
length_frame = 640 * 480 * 3
length_total = length_idhw + length_skel + length_frame

def view_stream():
    print("Press q to quit")
    read_a_frame = False
    with mmap.mmap(-1, length_total, tagname="jpysync") as data:
        while True:
            data.flush()
            data.seek(0)
            update_id = int(np.frombuffer(data.read(4), dtype=np.int32)[0])
            if update_id == -10 and read_a_frame:
                break
            else:
                if update_id != -10:
                    read_a_frame = True
            data.seek(length_idhw)
            buf = data.read(length_skel)
            kps = np.frombuffer(buf, dtype=np.float32).reshape((-1, 3))
            data.seek(length_idhw+length_skel)
            buf = data.read()
            img = np.frombuffer(buf, dtype=np.uint8).reshape((480, 640, 3))
            for (each_x, each_y, each_s) in kps:
                cv2.circle(img, (each_x, each_y), 5, (0, 255, 0), 3)
            cv2.imshow('read_image', img)
            if cv2.waitKey(16) & 0xFF == ord('q'):
                break



if __name__ == '__main__':
    view_stream()