export const speedPresets: Record<number, string> = {
  0: '最慢', 1: '更慢', 2: '慢', 3: '平衡',
  4: '快', 5: '更快', 6: '很快', 7: '超快', 8: '极快'
}

export const videoCodes = ['自动', 'H264', 'H265', 'VP9', 'AV1 (aom)', 'AV1 (SVT)']

export const audioCodes = ['自动', 'AAC', 'OPUS']

export const audioSamples = [8000, 16000, 32000, 44100, 48000, 96000]

export const aspectRatios = ['4:3', '16:9', '2.35']

export const fpses = [10, 24, 25, 29.97, 30, 59.94, 60]

export const sizes: Record<string, string> = {
  '480P': '-1:480', '720P': '-1:720', '1080P': '-1:1080',
  '1440P': '-1:1440', '2160P': '-1:2160'
}

export const pixelFormats = ['yuv420p', 'yuvj420p', 'yuv422p', 'yuvj422p', 'rgb24', 'gray', 'yuv420p10le']
