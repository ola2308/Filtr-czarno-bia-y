.data
    align 16
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0
    rounding_const REAL4 0.5, 0.5, 0.5, 0.5
    max_value REAL4 255.0, 255.0, 255.0, 255.0
    min_value REAL4 0.0, 0.0, 0.0, 0.0
    align 16
    r_weight REAL4 0.299, 0.299, 0.299, 0.299
    g_weight REAL4 0.587, 0.587, 0.587, 0.587
    b_weight REAL4 0.114, 0.114, 0.114, 0.114

.code
GrayscaleFilter PROC
    push rbp
    mov rbp, rsp
    push rsi
    push rdi
    push rbx

    mov rsi, rcx      
    mov rdi, rdx      
    mov ebx, r8d     

    shufps xmm3, xmm3, 0    

    movaps xmm7, XMMWORD PTR [rounding_const]  
    movaps xmm8, XMMWORD PTR [max_value]       
    movaps xmm9, XMMWORD PTR [min_value]       
    
   
    movaps xmm10, XMMWORD PTR [r_weight]      
    movaps xmm11, XMMWORD PTR [g_weight]      
    movaps xmm12, XMMWORD PTR [b_weight]       
    
    mulps xmm10, xmm3                         
    mulps xmm11, xmm3
    mulps xmm12, xmm3

    mov ecx, ebx
    shr ecx, 2         
    test ecx, ecx
    jz process_remaining

process_4_pixels:
    movzx eax, byte ptr [rsi]     
    cvtsi2ss xmm0, eax
    movzx eax, byte ptr [rsi+3]
    cvtsi2ss xmm1, eax
    movzx eax, byte ptr [rsi+6] 
    cvtsi2ss xmm2, eax
    movzx eax, byte ptr [rsi+9]  
    cvtsi2ss xmm4, eax
    insertps xmm0, xmm1, 010h
    insertps xmm0, xmm2, 020h
    insertps xmm0, xmm4, 030h
    
    mulps xmm0, xmm12 

    movzx eax, byte ptr [rsi+1] 
    cvtsi2ss xmm1, eax
    movzx eax, byte ptr [rsi+4] 
    cvtsi2ss xmm2, eax
    movzx eax, byte ptr [rsi+7]  
    cvtsi2ss xmm4, eax
    movzx eax, byte ptr [rsi+10]
    cvtsi2ss xmm5, eax
    insertps xmm1, xmm2, 010h
    insertps xmm1, xmm4, 020h
    insertps xmm1, xmm5, 030h
    
    mulps xmm1, xmm11             
    addps xmm0, xmm1             

   
    movzx eax, byte ptr [rsi+2]
    cvtsi2ss xmm1, eax
    movzx eax, byte ptr [rsi+5] 
    cvtsi2ss xmm2, eax
    movzx eax, byte ptr [rsi+8]  
    cvtsi2ss xmm4, eax
    movzx eax, byte ptr [rsi+11]  
    cvtsi2ss xmm5, eax
    insertps xmm1, xmm2, 010h
    insertps xmm1, xmm4, 020h
    insertps xmm1, xmm5, 030h
    
    mulps xmm1, xmm10      
    addps xmm0, xmm1     

    addps xmm0, xmm7              
    maxps xmm0, xmm9              
    minps xmm0, xmm8            

   
    cvtss2si eax, xmm0
    mov byte ptr [rdi], al
    mov byte ptr [rdi+1], al
    mov byte ptr [rdi+2], al
    
    psrldq xmm0, 4
    cvtss2si eax, xmm0
    mov byte ptr [rdi+3], al
    mov byte ptr [rdi+4], al
    mov byte ptr [rdi+5], al
    
    psrldq xmm0, 4
    cvtss2si eax, xmm0
    mov byte ptr [rdi+6], al
    mov byte ptr [rdi+7], al
    mov byte ptr [rdi+8], al
    
    psrldq xmm0, 4
    cvtss2si eax, xmm0
    mov byte ptr [rdi+9], al
    mov byte ptr [rdi+10], al
    mov byte ptr [rdi+11], al

    add rsi, 12                   
    add rdi, 12
    dec ecx
    jnz process_4_pixels

process_remaining:
    and ebx, 3                    
    jz cleanup

    movss xmm6, xmm3

remaining_loop:             
    movzx eax, byte ptr [rsi]; B
    cvtsi2ss xmm1, eax
    mulss xmm1, dword ptr [b_weight]
    mulss xmm1, xmm6              
    
    movzx eax, byte ptr [rsi+1]; G
    cvtsi2ss xmm2, eax
    mulss xmm2, dword ptr [g_weight]
    mulss xmm2, xmm6         
    addss xmm1, xmm2
    
    movzx eax, byte ptr [rsi+2]; R
    cvtsi2ss xmm2, eax
    mulss xmm2, dword ptr [r_weight]
    mulss xmm2, xmm6       
    addss xmm1, xmm2
    
    addss xmm1, dword ptr [rounding_const]
    maxss xmm1, dword ptr [min_value]
    minss xmm1, dword ptr [max_value]
    
    cvtss2si eax, xmm1
    mov byte ptr [rdi], al
    mov byte ptr [rdi+1], al
    mov byte ptr [rdi+2], al
    
    add rsi, 3
    add rdi, 3
    dec ebx
    jnz remaining_loop

cleanup:
    pop rbx
    pop rdi
    pop rsi
    pop rbp
    ret
GrayscaleFilter ENDP
END