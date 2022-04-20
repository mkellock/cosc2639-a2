import { Stack, Input, Button, Alert, AlertIcon, Link } from '@chakra-ui/react';
import { useState } from 'react';

export interface RegisterDetails {
    email: string;
    username: string;
    password: string;
}

interface InputProps {
    hidden?: boolean;
    inError?: boolean;
    errorMessage?: string;
    onSubmit(props: RegisterDetails): void;
    onLogin(): void;
}

export function Register(props: InputProps) {
    const [email, setEmail] = useState('');
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [inError, setInError] = useState<boolean>();
    const [errorMessage, setErrorMessage] = useState<string>();

    const submit = () => {
        setInError(false);
        setErrorMessage('');

        if (email.length === 0 || username.length === 0 || password.length === 0) {
            setInError(true);
            setErrorMessage('Please fill in all form fields');
        } else {
            const registerDetails: RegisterDetails = {
                email: email,
                username: username,
                password: password,
            };

            props.onSubmit(registerDetails);
        }
    };

    if (props.hidden === undefined || !props.hidden) {
        return (
            <>
                {inError || props.inError ? (
                    <Alert status="error">
                        <AlertIcon />
                        {errorMessage}
                        {props.errorMessage}
                    </Alert>
                ) : null}
                <Stack spacing={3} width={450} padding={3} hidden={false}>
                    <Input onChange={(e) => setEmail(e.target.value)} placeholder="EMail" />
                    <Input onChange={(e) => setUsername(e.target.value)} placeholder="Username" />
                    <Input onChange={(e) => setPassword(e.target.value)} placeholder="Password" type="password" />
                    <Button onClick={submit}>Register</Button>
                    <Link onClick={props.onLogin}>Login</Link>
                </Stack>
            </>
        );
    } else {
        return null;
    }
}

export default Register;
