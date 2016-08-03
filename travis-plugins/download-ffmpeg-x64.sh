#!/bin/bash -e
set -e
old=$(pwd)
dir=$(tempfile -pffd-)
file=$(tempfile -pff-)
mkdir -p $dir-files

echo download as $file.tar.xz
wget -O $file.tar.xz http://johnvansickle.com/ffmpeg/builds/ffmpeg-git-64bit-static.tar.xz

echo extract to: $dir-files
tar xJf $file.tar.xz -C $dir-files
rm $file.tar.xz

echo See $dir-files
ls -L $dir-files

mkdir -p $HOME/bin
cd $dir-files
cd ffmpeg*
cp -R * $HOME/bin
cd $pwd
rm -rf $dir-files
rm -f $file
rm -f $dir
cd $old
echo '************************ DONE: $HOME/bin/ffmpeg is ready *********************'
