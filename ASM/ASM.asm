.data
    align 16                        
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0
    
.code
GrayscaleFilter PROC
    push rbp
    mov rbp, rsp
    push rsi
    push rdi
    
    mov rsi, rcx                   ; input buffer
    mov rdi, rdx                   ; output buffer
    mov r9d, r8d                   ; copy of pixelCount
    mov ecx, r8d                   ; counter for main loop
    
    movups xmm0, [rgb_weights]     
    shufps xmm3, xmm3, 0          
    mulps xmm0, xmm3              
    
    movaps xmm4, xmm0             ; Blue weights
    movaps xmm5, xmm0             ; Green weights
    movaps xmm6, xmm0             ; Red weights
    
    shufps xmm4, xmm4, 0          
    shufps xmm5, xmm5, 01010101b  
    shufps xmm6, xmm6, 10101010b  
    
    pxor xmm7, xmm7               ; Zero register for unpacking
    
    ; Process blocks of 4 pixels
    mov eax, ecx
    shr ecx, 2                    ; divide by 4 for main loop
    and eax, 3                    ; save remainder
    push rax                      ; save remainder for later
    
    test ecx, ecx
    jz process_remaining

process_pixels:
    ; Load 12 bytes (4 pixels * 3 colors)
    movq xmm1, qword ptr [rsi]    ; Load first 8 bytes
    movd xmm2, dword ptr [rsi+8]  ; Load remaining 4 bytes
    
    ; Unpack bytes to words
    punpcklbw xmm1, xmm7          
    punpcklbw xmm2, xmm7          
    
    ; Unpack words to dwords
    punpcklwd xmm1, xmm7          
    punpcklwd xmm2, xmm7          
    
    ; Convert to float
    cvtdq2ps xmm1, xmm1           
    cvtdq2ps xmm2, xmm2           
    
    ; Process RGB channels
    mulps xmm1, xmm4              ; Blue
    mulps xmm2, xmm5              ; Green
    addps xmm1, xmm2              ; Add green to blue
    
    movd xmm2, dword ptr [rsi+8]  ; Load red channel
    punpcklbw xmm2, xmm7
    punpcklwd xmm2, xmm7
    cvtdq2ps xmm2, xmm2
    mulps xmm2, xmm6              ; Red
    addps xmm1, xmm2              ; Add red
    
    ; Round to nearest
    roundps xmm1, xmm1, 0         
    
    ; Convert back to integers
    cvtps2dq xmm1, xmm1           
    
    ; Pack with saturation
    packssdw xmm1, xmm1           
    packuswb xmm1, xmm1           
    
    ; Extract result
    movd eax, xmm1                
    
    ; Store 4 pixels (each 3 bytes)
    mov r10d, eax                 ; Save result
    mov byte ptr [rdi], al        ; First pixel
    mov byte ptr [rdi+1], al
    mov byte ptr [rdi+2], al
    shr eax, 8
    mov byte ptr [rdi+3], al      ; Second pixel
    mov byte ptr [rdi+4], al
    mov byte ptr [rdi+5], al
    shr eax, 8
    mov byte ptr [rdi+6], al      ; Third pixel
    mov byte ptr [rdi+7], al
    mov byte ptr [rdi+8], al
    shr eax, 8
    mov byte ptr [rdi+9], al      ; Fourth pixel
    mov byte ptr [rdi+10], al
    mov byte ptr [rdi+11], al
    
    ; Move to next block
    add rsi, 12                   ; Advance input pointer (4 pixels * 3 bytes)
    add rdi, 12                   ; Advance output pointer
    
    dec ecx
    jnz process_pixels

process_remaining:
    pop rax                       ; Get remainder count
    test rax, rax
    jz cleanup

remaining_loop:
    ; Process single pixel
    movzx r10d, byte ptr [rsi]    ; Blue
    cvtsi2ss xmm1, r10d
    mulss xmm1, xmm4
    
    movzx r10d, byte ptr [rsi+1]  ; Green
    cvtsi2ss xmm2, r10d
    mulss xmm2, xmm5
    addss xmm1, xmm2
    
    movzx r10d, byte ptr [rsi+2]  ; Red
    cvtsi2ss xmm2, r10d
    mulss xmm2, xmm6
    addss xmm1, xmm2
    
    roundss xmm1, xmm1, 0
    cvtss2si r10d, xmm1
    
    mov byte ptr [rdi], r10b
    mov byte ptr [rdi+1], r10b
    mov byte ptr [rdi+2], r10b
    
    add rsi, 3
    add rdi, 3
    
    dec rax
    jnz remaining_loop

cleanup:
    pop rdi
    pop rsi
    pop rbp
    ret
GrayscaleFilter ENDP
END