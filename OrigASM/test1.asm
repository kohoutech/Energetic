; OriGASM test1 source file
; c = a + b

.segment data 
  a dd 60 		; first int
  b dd 9 		; second int

.segment bss
  c dd  		; result

.segment text
.global test1

  test1:        
    push ebp
    mov ebp,esp
    mov eax, [a]
    add eax, [b]
    mov [c], eax
    mov eax, [c]	; return result
    mov esp,ebp
    pop ebp 
    ret
