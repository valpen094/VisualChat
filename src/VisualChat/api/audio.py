from fastapi import APIRouter
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from typing import List
from faster_whisper import WhisperModel

import datetime
import sounddevice as sd
import numpy as np
import scipy.io.wavfile as wav
import time

MODEL_SIZE = "small" # small, medium, large

print(f"{datetime.datetime.now()} GPU is not available.")
model = WhisperModel(MODEL_SIZE, device = "cpu", compute_type=  "float32")

router = APIRouter()

class FilePathModel(BaseModel): filePath: str

# Record audio
@router.post('/record')
async def recordAsync(data: FilePathModel):
    file_path = data.filePath

    SAMPLE_RATE = 44000
    THRESHOLD = 500
    SILENCE_DURATION = 1

    audio_data = []
    start_time = time.time()
    recoginazed_voice = False

    print(file_path)
    print(f"{datetime.datetime.now()} Start recording.")

    while True:
        # Get audio every 100ms
        chunk = sd.rec(int(SAMPLE_RATE * 0.1), samplerate = SAMPLE_RATE, channels = 1, dtype = np.int16)
        sd.wait()
    
        # Calculating volume
        volume = np.abs(chunk).mean()

        # Save a data (After initial recognition)
        if recoginazed_voice:
            audio_data.append(chunk)

        # Silence detection
        if volume < THRESHOLD:
            if time.time() - start_time > SILENCE_DURATION:
                if recoginazed_voice: # If there is no sound after recording starts, the condition will not be met.
                    print(f"{datetime.datetime.now()} Stop recording.")
                    break

        else:
            # Save a data (Initial recognition)
            if recoginazed_voice is False:
                audio_data.append(chunk)

            start_time = time.time()
            recoginazed_voice = True

    audio_data = np.concatenate(audio_data, axis = 0)
    print(f"{datetime.datetime.now()}" + " " + file_path)

    # Save the file
    wav.write(file_path, SAMPLE_RATE, audio_data)
    return JSONResponse(content = {"content": file_path})

# Transcribe audio
@router.post('/transcribe')
async def transcribeAsync(data: FilePathModel):
    file_path = data.filePath

    # Transcription of audio files
    print(f"{datetime.datetime.now()} Start transcription.")
    segments, info = model.transcribe(file_path, beam_size = 5)
    print(f"{datetime.datetime.now()} Transcription completed.")

    # Transcription results
    message = ""

    '''
    for segment in segments:
        print(f"{datetime.datetime.now()} [{segment.start:.2f}s - {segment.end:.2f}s] {segment.text}")
        message += segment.text
    
    return JSONResponse(content = {"content": message})
    '''

    segment_data = [{"start": segment.start, "end": segment.end, "text": segment.text} for segment in segments]
    return JSONResponse(content = {"segments": segment_data})

# Record and transcribe
@router.post('/whisper')
async def whisperAsync(data: FilePathModel):
    file_path = data.filePath

    '''
    Recording
    '''

    SAMPLE_RATE = 44000
    THRESHOLD = 500
    SILENCE_DURATION = 1

    audio_data = []
    start_time = time.time()
    recoginazed_voice = False

    print(f"{datetime.datetime.now()} Start recording.")

    while True:
        # Get audio every 100ms
        chunk = sd.rec(int(SAMPLE_RATE * 0.1), samplerate = SAMPLE_RATE, channels = 1, dtype = np.int16)
        sd.wait()
    
        # Calculating volume
        volume = np.abs(chunk).mean()

        # Save a data (After initial recognition)
        if recoginazed_voice:
            audio_data.append(chunk)

        # Silence detection
        if volume < THRESHOLD:
            if time.time() - start_time > SILENCE_DURATION:
                if recoginazed_voice: # If there is no sound after recording starts, the condition will not be met.
                    print(f"{datetime.datetime.now()} Stop recording.")
                    break

        else:
            # Save a data (Initial recognition)
            if recoginazed_voice is False:
                audio_data.append(chunk)

            start_time = time.time()
            recoginazed_voice = True

    audio_data = np.concatenate(audio_data, axis = 0)
    print(f"{datetime.datetime.now()}" + " " + file_path)

    # Save the file
    wav.write(file_path, SAMPLE_RATE, audio_data)
    
    '''
    Transcription
    '''
    
    # Transcription of audio file
    print(f"{datetime.datetime.now()} Start transcription.")
    segments, info = model.transcribe(file_path, beam_size=5)
    print(f"{datetime.datetime.now()} Transcription completed.")

    # Transcription results
    message = ""

    '''
    for segment in segments:
        print(f"{datetime.datetime.now()} [{segment.start:.2f}s - {segment.end:.2f}s] {segment.text}")
        message += segment.text
    
    return JSONResponse(content = {"content": message})
    '''

    segment_data = [{"start": segment.start, "end": segment.end, "text": segment.text} for segment in segments]
    return JSONResponse(content = {"segments": segment_data})