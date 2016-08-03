#!/bin/bash
file=/tmp/big_buck_bunny_1080p_H264_AAC_25fps_7200K.mp4
wget -O $file http://download.openbricks.org/sample/H264/big_buck_bunny_1080p_H264_AAC_25fps_7200K.MP4
$HOME/bin/ffprobe $file
sudo apt-get install pv -y > /dev/null 2>&1

$HOME/bin/ffmpeg -i $file -y \
  -map 0:0 -map 0:1  \
  -c:v libx265 \
  -preset medium \
  -scodec copy -threads 3 -strict experimental \
  -c:a copy \
  -x265-params crf=22:crf-min=15:crf-max=22:pools=4 \
  - | pv > $file.mkv

echo '************* convert done ****************'
echo '


'

$HOME/bin/ffprobe  $file.mkv
