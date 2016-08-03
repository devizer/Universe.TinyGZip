#!/bin/bash
file=/tmp/big_buck_bunny_1080p_H264_AAC_25fps_7200K.mp4
wget -O $file http://download.openbricks.org/sample/H264/big_buck_bunny_1080p_H264_AAC_25fps_7200K.MP4
$HOME/bin/ffprobe $file

