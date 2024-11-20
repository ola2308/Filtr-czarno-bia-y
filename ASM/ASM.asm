.data
    align 16
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0
    mask_blue DD 0FFh, 0FFh, 0FFh, 0FFh
    align 16

.code
GrayscaleFilter PROC
    ; Function prologue
    push rbp
    mov rbp, rsp
    
    ; Preserve non-volatile registers
    push rbx
    push r12
    push r13
    push r14
    push r15

    ; Parameters:
    ; rcx = input buffer
    ; rdx = output buffer
    ; r8 = pixel count
    
    ; Store parameters in non-volatile registers
    mov r12, rcx    ; Input buffer
    mov r13, rdx    ; Output buffer
    mov r14, r8     ; Pixel count
    
    ; Load RGB weights into xmm15 (non-volatile)
    movups xmm15, rgb_weights

pixel_loop:
    ; Check if we've processed all pixels
    test r14, r14
    jz ending
    
    ; Load 3 bytes (1 pixel) from input
    movzx eax, BYTE PTR [r12]      ; Blue
    movzx ebx, BYTE PTR [r12 + 1]  ; Green
    movzx ecx, BYTE PTR [r12 + 2]  ; Red
    
    ; Convert to float
    cvtsi2ss xmm0, eax
    cvtsi2ss xmm1, ebx
    cvtsi2ss xmm2, ecx
    
    ; Multiply by weights
    mulss xmm0, xmm15              ; Blue * 0.114
    mulss xmm1, DWORD PTR [rgb_weights + 4]  ; Green * 0.587
    mulss xmm2, DWORD PTR [rgb_weights + 8]  ; Red * 0.299
    
    ; Sum the weighted values
    addss xmm0, xmm1
    addss xmm0, xmm2
    
    ; Convert back to integer
    cvtss2si eax, xmm0
    
    ; Write the same value to all three channels
    mov BYTE PTR [r13], al
    mov BYTE PTR [r13 + 1], al
    mov BYTE PTR [r13 + 2], al
    
    ; Move to next pixel
    add r12, 3      ; Input buffer (RGB = 3 bytes)
    add r13, 3      ; Output buffer
    dec r14         ; Decrement pixel count
    jmp pixel_loop

ending:
    ; Restore non-volatile registers
    pop r15
    pop r14
    pop r13
    pop r12
    pop rbx
    
    ; Function epilogue
    mov rsp, rbp
    pop rbp
    ret
GrayscaleFilter ENDP

END